using System.Reflection;
using Microsoft.Extensions.Logging;
using NLua;

namespace Lua2CS;

public sealed class LuaRuntime(ILogger logger, bool allowUnsafeLibraries)
{
    private const string Bootstrap = """
        local create_plugin = __lua2cs_create_plugin
        local register_event = __lua2cs_register_event
        local register_listener = __lua2cs_register_listener
        local register_command = __lua2cs_register_command
        local register_timer = __lua2cs_register_timer
        local register_load = __lua2cs_register_load
        local register_unload = __lua2cs_register_unload
        local cancel_registration = __lua2cs_cancel_registration
        local log_debug = __lua2cs_log_debug
        local log_info = __lua2cs_log_info
        local log_warn = __lua2cs_log_warn
        local log_error = __lua2cs_log_error
        local server_chat = __lua2cs_server_chat
        local server_console = __lua2cs_server_console
        local server_command = __lua2cs_server_command
        local players_all = __lua2cs_players_all
        local players_get = __lua2cs_players_get
        local player_chat = __lua2cs_player_chat
        local player_console = __lua2cs_player_console
        local player_center = __lua2cs_player_center
        local command_reply = __lua2cs_command_reply

        cs = {
            continue = "continue",
            changed = "changed",
            handled = "handled",
            stop = "stop",
            log = {
                debug = log_debug,
                info = log_info,
                warn = log_warn,
                error = log_error
            },
            server = {
                print_chat_all = server_chat,
                print_console = server_console,
                execute = server_command
            },
            players = {
                all = players_all,
                get = players_get
            },
            colors = {
                default = "\x01",
                white = "\x01",
                dark_red = "\x02",
                light_purple = "\x03",
                green = "\x04",
                olive = "\x05",
                lime = "\x06",
                red = "\x07",
                grey = "\x08",
                yellow = "\x09",
                silver = "\x0A",
                blue = "\x0B",
                dark_blue = "\x0C",
                purple = "\x0E",
                light_red = "\x0F",
                gold = "\x10",
                orange = "\x10"
            }
        }

        function cs.plugin(spec)
            create_plugin(spec)
            local plugin = {}

            function plugin:on(name, callback, options)
                return register_event(name, callback, options or {})
            end

            function plugin:listen(name, callback)
                return register_listener(name, callback)
            end

            function plugin:command(name, options, callback)
                if type(options) == "function" then
                    callback = options
                    options = {}
                end
                return register_command(name, options or {}, callback)
            end

            function plugin:timer(interval, callback, options)
                return register_timer(interval, callback, options or {})
            end

            function plugin:on_load(callback)
                register_load(callback)
            end

            function plugin:on_unload(callback)
                register_unload(callback)
            end

            function plugin:cancel(registration_id)
                return cancel_registration(registration_id)
            end

            return plugin
        end

        function __lua2cs_player_print_chat(self, message)
            return player_chat(self.slot, message)
        end

        function __lua2cs_player_print_console(self, message)
            return player_console(self.slot, message)
        end

        function __lua2cs_player_print_center(self, message)
            return player_center(self.slot, message)
        end

        function __lua2cs_command_reply_method(self, message)
            return command_reply(self.__context_id, message)
        end

        package.path = __lua2cs_module_path .. "/?.lua;" .. __lua2cs_module_path .. "/?/init.lua"
        package.cpath = ""
        luanet = nil
        __lua2cs_create_plugin = nil
        __lua2cs_register_event = nil
        __lua2cs_register_listener = nil
        __lua2cs_register_command = nil
        __lua2cs_register_timer = nil
        __lua2cs_register_load = nil
        __lua2cs_register_unload = nil
        __lua2cs_cancel_registration = nil
        __lua2cs_log_debug = nil
        __lua2cs_log_info = nil
        __lua2cs_log_warn = nil
        __lua2cs_log_error = nil
        __lua2cs_server_chat = nil
        __lua2cs_server_console = nil
        __lua2cs_server_command = nil
        __lua2cs_players_all = nil
        __lua2cs_players_get = nil
        __lua2cs_player_chat = nil
        __lua2cs_player_console = nil
        __lua2cs_player_center = nil
        __lua2cs_command_reply = nil
        """;

    public LuaPlugin Prepare(string scriptPath)
    {
        var fullPath = Path.GetFullPath(scriptPath);
        var state = new Lua { UseTraceback = true };
        LuaPlugin? plugin = null;

        try
        {
            plugin = new LuaPlugin(fullPath, state, logger);
            var api = new LuaApi(plugin, logger);
            plugin.Api = api;

            RegisterFunctions(state, api);
            state["__lua2cs_module_path"] = Path.GetDirectoryName(fullPath)!;
            state.DoString(Bootstrap, "@lua2cs/bootstrap.lua");
            api.InitializeLuaMethods();

            if (!allowUnsafeLibraries)
            {
                state.DoString(
                    "dofile=nil; loadfile=nil; io=nil; os.execute=nil; os.remove=nil; os.rename=nil; package.loadlib=nil",
                    "@lua2cs/sandbox.lua");
            }

            state.DoFile(fullPath);
            if (string.IsNullOrWhiteSpace(plugin.Name))
            {
                throw new InvalidDataException("The script must call cs.plugin({ name = ... }).");
            }

            return plugin;
        }
        catch
        {
            plugin?.Dispose();
            if (plugin is null)
            {
                state.Dispose();
            }

            throw;
        }
    }

    private static void RegisterFunctions(Lua state, LuaApi api)
    {
        Register(state, api, "__lua2cs_create_plugin", nameof(LuaApi.CreatePlugin));
        Register(state, api, "__lua2cs_register_event", nameof(LuaApi.RegisterEvent));
        Register(state, api, "__lua2cs_register_listener", nameof(LuaApi.RegisterListener));
        Register(state, api, "__lua2cs_register_command", nameof(LuaApi.RegisterCommand));
        Register(state, api, "__lua2cs_register_timer", nameof(LuaApi.RegisterTimer));
        Register(state, api, "__lua2cs_register_load", nameof(LuaApi.RegisterLoad));
        Register(state, api, "__lua2cs_register_unload", nameof(LuaApi.RegisterUnload));
        Register(state, api, "__lua2cs_cancel_registration", nameof(LuaApi.CancelRegistration));
        Register(state, api, "__lua2cs_log_debug", nameof(LuaApi.LogDebug));
        Register(state, api, "__lua2cs_log_info", nameof(LuaApi.LogInfo));
        Register(state, api, "__lua2cs_log_warn", nameof(LuaApi.LogWarning));
        Register(state, api, "__lua2cs_log_error", nameof(LuaApi.LogError));
        Register(state, api, "__lua2cs_server_chat", nameof(LuaApi.ServerPrintChatAll));
        Register(state, api, "__lua2cs_server_console", nameof(LuaApi.ServerPrintConsole));
        Register(state, api, "__lua2cs_server_command", nameof(LuaApi.ServerExecute));
        Register(state, api, "__lua2cs_players_all", nameof(LuaApi.GetPlayers));
        Register(state, api, "__lua2cs_players_get", nameof(LuaApi.GetPlayer));
        Register(state, api, "__lua2cs_player_chat", nameof(LuaApi.PlayerPrintChat));
        Register(state, api, "__lua2cs_player_console", nameof(LuaApi.PlayerPrintConsole));
        Register(state, api, "__lua2cs_player_center", nameof(LuaApi.PlayerPrintCenter));
        Register(state, api, "__lua2cs_command_reply", nameof(LuaApi.CommandReply));
    }

    private static void Register(Lua state, LuaApi api, string luaName, string methodName)
    {
        var method = typeof(LuaApi).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
                     ?? throw new MissingMethodException(typeof(LuaApi).FullName, methodName);
        state.RegisterFunction(luaName, api, method);
    }
}
