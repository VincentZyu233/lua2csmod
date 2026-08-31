using Microsoft.Extensions.Logging.Abstractions;

namespace Lua2CS.Tests;

public sealed class LuaRuntimeTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "lua2cs-tests", Guid.NewGuid().ToString("N"));

    public LuaRuntimeTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void PrepareLoadsMetadataAndRegistrationDefinitions()
    {
        var path = WriteScript("sample.lua", """
            local plugin = cs.plugin({
                name = "Sample",
                version = "1.2.3",
                description = "test plugin"
            })

            plugin:on("player_death", function(event, info)
                return cs.continue
            end, { mode = "pre" })

            plugin:listen("OnMapStart", function(map_name) end)
            plugin:command("css_sample", {
                description = "sample command",
                permission = "@css/generic",
                min_args = 1,
                usage = "<value>"
            }, function(player, command) end)
            plugin:timer(1.5, function() end, { repeating = true })
            """);

        using var plugin = new LuaRuntime(NullLogger.Instance, false).Prepare(path);

        Assert.Equal("Sample", plugin.Name);
        Assert.Equal("1.2.3", plugin.Version);
        Assert.Equal("test plugin", plugin.Description);
        Assert.Collection(
            plugin.Registrations,
            item => Assert.IsType<EventRegistration>(item),
            item => Assert.IsType<ListenerRegistration>(item),
            item => Assert.IsType<CommandRegistration>(item),
            item => Assert.IsType<TimerRegistration>(item));

        var gameEvent = Assert.IsType<EventRegistration>(plugin.Registrations[0]);
        Assert.Equal(CounterStrikeSharp.API.Core.HookMode.Pre, gameEvent.Mode);
        var command = Assert.IsType<CommandRegistration>(plugin.Registrations[2]);
        Assert.Equal(1, command.MinArgs);
        Assert.Equal("@css/generic", command.Permission);
    }

    [Fact]
    public void PrepareLoadsModulesFromTheScriptDirectory()
    {
        WriteScript("helper.lua", "return { version = '2.0.0' }");
        var path = WriteScript("module_user.lua", """
            local helper = require("helper")
            cs.plugin({ name = "Module User", version = helper.version })
            """);

        using var plugin = new LuaRuntime(NullLogger.Instance, false).Prepare(path);

        Assert.Equal("2.0.0", plugin.Version);
    }

    [Fact]
    public void SafeRuntimeRemovesClrAndUnsafeFileApis()
    {
        var path = WriteScript("safe.lua", """
            assert(luanet == nil)
            assert(io == nil)
            assert(dofile == nil)
            assert(loadfile == nil)
            assert(os.execute == nil)
            assert(package.loadlib == nil)
            cs.plugin({ name = "Safe" })
            """);

        using var plugin = new LuaRuntime(NullLogger.Instance, false).Prepare(path);
        Assert.Equal("Safe", plugin.Name);
    }

    [Fact]
    public void PrepareRejectsScriptsWithoutPluginMetadata()
    {
        var path = WriteScript("invalid.lua", "return true");
        var exception = Assert.Throws<InvalidDataException>(() =>
            new LuaRuntime(NullLogger.Instance, false).Prepare(path));
        Assert.Contains("cs.plugin", exception.Message);
    }

    [Fact]
    public void LifecycleCallbacksExecute()
    {
        var path = WriteScript("lifecycle.lua", """
            local plugin = cs.plugin({ name = "Lifecycle" })
            plugin:on_load(function(hot_reload)
                loaded = hot_reload and 2 or 1
            end)
            plugin:on_unload(function(hot_reload)
                unloaded = hot_reload and 2 or 1
            end)
            """);

        using var plugin = new LuaRuntime(NullLogger.Instance, false).Prepare(path);
        plugin.InvokeLifecycle(plugin.LoadCallback, true);
        plugin.InvokeLifecycle(plugin.UnloadCallback, false);

        Assert.Equal(2, plugin.State.GetInteger("loaded"));
        Assert.Equal(1, plugin.State.GetInteger("unloaded"));
    }

    [Fact]
    public void RegistrationCanBeCancelledDuringPreparation()
    {
        var path = WriteScript("cancel.lua", """
            local plugin = cs.plugin({ name = "Cancel" })
            local id = plugin:timer(5, function() end)
            assert(plugin:cancel(id))
            """);

        using var plugin = new LuaRuntime(NullLogger.Instance, false).Prepare(path);
        Assert.Empty(plugin.Registrations);
    }

    [Fact]
    public void CommandOptionsCanBeOmitted()
    {
        var path = WriteScript("short_command.lua", """
            local plugin = cs.plugin({ name = "Short Command" })
            plugin:command("css_short", function(player, command) end)
            """);

        using var plugin = new LuaRuntime(NullLogger.Instance, false).Prepare(path);
        Assert.Single(plugin.Registrations);
        Assert.IsType<CommandRegistration>(plugin.Registrations[0]);
    }

    [Fact]
    public void FailedDynamicRegistrationIsRemovedAgain()
    {
        var path = WriteScript("dynamic_failure.lua", """
            local plugin = cs.plugin({ name = "Dynamic Failure" })
            plugin:on_load(function()
                plugin:timer(1, function() end)
            end)
            """);

        using var plugin = new LuaRuntime(NullLogger.Instance, false).Prepare(path);
        plugin.Activate(
            definition => new RegistrationHandle(definition.Id, () => { }),
            _ => throw new InvalidOperationException("activation rejected"));

        Assert.ThrowsAny<Exception>(() => plugin.InvokeLifecycle(plugin.LoadCallback, false));
        Assert.Empty(plugin.Registrations);
    }

    [Fact]
    public void DeactivationContinuesAfterOneHandleFails()
    {
        var path = WriteScript("cleanup.lua", """
            local plugin = cs.plugin({ name = "Cleanup" })
            plugin:timer(1, function() end)
            plugin:timer(2, function() end)
            """);
        var disposed = 0;

        using var plugin = new LuaRuntime(NullLogger.Instance, false).Prepare(path);
        plugin.Activate(definition => new RegistrationHandle(definition.Id, () =>
        {
            if (definition.Id == 2) throw new InvalidOperationException("cleanup failed");
            disposed++;
        }));

        plugin.Deactivate();
        Assert.Equal(1, disposed);
        Assert.False(plugin.IsActive);
    }

    [Theory]
    [InlineData("hello.lua")]
    [InlineData("qwq.lua")]
    [InlineData("round_timer.lua")]
    public void ShippedExamplesLoad(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "examples", fileName);
        using var plugin = new LuaRuntime(NullLogger.Instance, false).Prepare(path);
        Assert.False(string.IsNullOrWhiteSpace(plugin.Name));
    }

    [Theory]
    [InlineData("DmgArmor", "dmg_armor")]
    [InlineData("Userid", "userid")]
    [InlineData("WeaponFauxitemid", "weapon_fauxitemid")]
    public void EventFieldNamesBecomeSnakeCase(string source, string expected) =>
        Assert.Equal(expected, LuaApi.ToSnakeCase(source));

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("hello.lua", "hello")]
    [InlineData("  fun_plugin.lua  ", "fun_plugin")]
    public void PluginKeysAreNormalized(string source, string expected) =>
        Assert.Equal(expected, LuaPluginManager.NormalizeKey(source));

    private string WriteScript(string name, string content)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
