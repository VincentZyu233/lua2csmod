using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace Lua2CS;

[MinimumApiVersion(373)]
public sealed class Lua2CSPlugin : BasePlugin, IPluginConfig<Lua2CSConfig>
{
    private LuaPluginManager? _manager;
    private HotReload? _hotReload;

    public override string ModuleName => "Lua2CS";
    public override string ModuleVersion => "0.1.0";
    public override string ModuleDescription => "Lua 5.4 plugin host for CounterStrikeSharp";
    public Lua2CSConfig Config { get; set; } = new();

    public void OnConfigParsed(Lua2CSConfig config)
    {
        if (Path.IsPathRooted(config.ScriptsDirectory))
        {
            throw new InvalidDataException("ScriptsDirectory must be relative to the Lua2CS plugin directory.");
        }

        config.ReloadDebounceMilliseconds = Math.Clamp(config.ReloadDebounceMilliseconds, 100, 5000);
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        var scriptsDirectory = ResolveScriptsDirectory();
        _manager = new LuaPluginManager(this, Logger, scriptsDirectory, Config.AllowUnsafeLibraries);
        AddCommand("css_lua", "Manage Lua2CS scripts", OnLuaCommand);

        foreach (var result in _manager.LoadAll().Where(result => !result.Success))
        {
            Logger.LogError("Lua startup load failed for {Script}: {Message}", result.Key, result.Message);
        }

        if (Config.AutoReload)
        {
            _hotReload = new HotReload(_manager, Logger, Config.ReloadDebounceMilliseconds, Server.NextWorldUpdate);
        }

        Logger.LogInformation("Lua2CS loaded with {Count} Lua plugin(s) from {Directory}", _manager.Plugins.Count, scriptsDirectory);
    }

    public override void Unload(bool hotReload)
    {
        _hotReload?.Dispose();
        _hotReload = null;
        _manager?.Shutdown(hotReload);
        _manager = null;
    }

    private void OnLuaCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (_manager is null) return;
        if (player is not null && !string.IsNullOrWhiteSpace(Config.AdminPermission)
                               && !AdminManager.PlayerHasPermissions(player, Config.AdminPermission))
        {
            command.ReplyToCommand("You do not have permission to manage Lua plugins.");
            return;
        }

        var action = command.ArgCount > 1 ? command.GetArg(1).Trim().ToLowerInvariant() : "list";
        try
        {
            switch (action)
            {
                case "list":
                    ReplyWithPluginList(command);
                    break;
                case "load":
                    Reply(command, command.ArgCount > 2 ? _manager.Load(command.GetArg(2)) : MissingName());
                    break;
                case "reload":
                case "restart":
                    Reply(command, command.ArgCount > 2 ? _manager.Reload(command.GetArg(2)) : MissingName());
                    break;
                case "unload":
                case "stop":
                    Reply(command, command.ArgCount > 2 ? _manager.Unload(command.GetArg(2)) : MissingName());
                    break;
                case "reload_all":
                    foreach (var result in _manager.ReloadAll()) Reply(command, result);
                    break;
                default:
                    command.ReplyToCommand("Usage: css_lua [list|load|reload|unload|reload_all] [script]");
                    break;
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Lua management command failed");
            command.ReplyToCommand($"[Lua2CS] Error: {exception.Message}");
        }
    }

    private void ReplyWithPluginList(CommandInfo command)
    {
        if (_manager!.Plugins.Count == 0)
        {
            command.ReplyToCommand("[Lua2CS] No Lua plugins are loaded.");
            return;
        }

        foreach (var plugin in _manager.Plugins.OrderBy(plugin => plugin.Key, StringComparer.OrdinalIgnoreCase))
        {
            command.ReplyToCommand($"[Lua2CS] {plugin.Key}: {plugin.Name} v{plugin.Version} ({plugin.Registrations.Count} registrations)");
        }
    }

    private string ResolveScriptsDirectory()
    {
        var moduleDirectory = Path.GetFullPath(ModuleDirectory);
        var scriptsDirectory = Path.GetFullPath(Path.Combine(moduleDirectory, Config.ScriptsDirectory));
        if (!scriptsDirectory.StartsWith(moduleDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidDataException("ScriptsDirectory escapes the Lua2CS plugin directory.");
        }
        return scriptsDirectory;
    }

    private static void Reply(CommandInfo command, PluginOperationResult result) =>
        command.ReplyToCommand($"[Lua2CS] {(result.Success ? "OK" : "FAILED")}: {result.Message}");

    private static PluginOperationResult MissingName() => PluginOperationResult.Fail(string.Empty, "A script name is required.");
}
