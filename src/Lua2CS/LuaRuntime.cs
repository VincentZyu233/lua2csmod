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
        local server_info = __lua2cs_server_info
        local server_maps = __lua2cs_server_maps
        local server_is_map_valid = __lua2cs_server_is_map_valid
        local server_precache_model = __lua2cs_server_precache_model
        local convar_get = __lua2cs_convar_get
        local convar_set = __lua2cs_convar_set
        local capability_events = __lua2cs_capability_events
        local capability_listeners = __lua2cs_capability_listeners
        local players_all = __lua2cs_players_all
        local players_get = __lua2cs_players_get
        local players_get_userid = __lua2cs_players_get_userid
        local players_get_steamid = __lua2cs_players_get_steamid
        local players_find = __lua2cs_players_find
        local players_humans = __lua2cs_players_humans
        local players_bots = __lua2cs_players_bots
        local players_count = __lua2cs_players_count
        local player_chat = __lua2cs_player_chat
        local player_console = __lua2cs_player_console
        local player_center = __lua2cs_player_center
        local player_alert = __lua2cs_player_alert
        local player_html = __lua2cs_player_html
        local player_refresh = __lua2cs_player_refresh
        local player_permission = __lua2cs_player_permission
        local player_can_target = __lua2cs_player_can_target
        local player_convar = __lua2cs_player_convar
        local player_execute = __lua2cs_player_execute
        local player_give_item = __lua2cs_player_give_item
        local player_remove_item = __lua2cs_player_remove_item
        local player_remove_weapons = __lua2cs_player_remove_weapons
        local player_drop_weapon = __lua2cs_player_drop_weapon
        local player_respawn = __lua2cs_player_respawn
        local player_kill = __lua2cs_player_kill
        local player_kick = __lua2cs_player_kick
        local player_change_team = __lua2cs_player_change_team
        local player_teleport = __lua2cs_player_teleport
        local player_set_health = __lua2cs_player_set_health
        local player_set_armor = __lua2cs_player_set_armor
        local player_set_money = __lua2cs_player_set_money
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
                execute = server_command,
                info = server_info,
                maps = server_maps,
                is_map_valid = server_is_map_valid,
                precache_model = server_precache_model
            },
            players = {
                all = players_all,
                get = players_get,
                get_userid = players_get_userid,
                get_steamid = players_get_steamid,
                find = players_find,
                humans = players_humans,
                bots = players_bots,
                count = players_count
            },
            convars = {
                get = convar_get,
                set = convar_set
            },
            capabilities = {
                events = capability_events,
                listeners = capability_listeners
            },
            team = {
                none = 0,
                spectator = 1,
                terrorist = 2,
                t = 2,
                counter_terrorist = 3,
                ct = 3
            },
            buttons = {
                attack = 1 << 0,
                jump = 1 << 1,
                duck = 1 << 2,
                forward = 1 << 3,
                back = 1 << 4,
                use = 1 << 5,
                left = 1 << 7,
                right = 1 << 8,
                move_left = 1 << 9,
                move_right = 1 << 10,
                attack2 = 1 << 11,
                reload = 1 << 13,
                speed = 1 << 16,
                walk = 1 << 17,
                zoom = 1 << 18,
                scoreboard = 1 << 33,
                inspect = 1 << 35
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

        function cs.vec3(x, y, z)
            return { x = x, y = y, z = z, [1] = x, [2] = y, [3] = z }
        end

        cs.angle = cs.vec3

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

            function plugin:after(delay, callback, options)
                options = options or {}
                options.repeating = false
                return register_timer(delay, callback, options)
            end

            function plugin:every(interval, callback, options)
                options = options or {}
                options.repeating = true
                return register_timer(interval, callback, options)
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

        function __lua2cs_player_print_alert(self, message)
            return player_alert(self.slot, message)
        end

        function __lua2cs_player_print_html(self, message, duration)
            return player_html(self.slot, message, duration or 5)
        end

        function __lua2cs_player_refresh_method(self)
            return player_refresh(self.slot)
        end

        function __lua2cs_player_has_permission_method(self, permission)
            return player_permission(self.slot, permission)
        end

        function __lua2cs_player_can_target_method(self, target)
            return target ~= nil and player_can_target(self.slot, target.slot)
        end

        function __lua2cs_player_get_convar_method(self, name)
            return player_convar(self.slot, name)
        end

        function __lua2cs_player_execute_method(self, command)
            return player_execute(self.slot, command, false)
        end

        function __lua2cs_player_execute_server_method(self, command)
            return player_execute(self.slot, command, true)
        end

        function __lua2cs_player_give_item_method(self, designer_name)
            return player_give_item(self.slot, designer_name)
        end

        function __lua2cs_player_remove_item_method(self, designer_name)
            return player_remove_item(self.slot, designer_name)
        end

        function __lua2cs_player_remove_weapons_method(self)
            return player_remove_weapons(self.slot)
        end

        function __lua2cs_player_drop_weapon_method(self)
            return player_drop_weapon(self.slot)
        end

        function __lua2cs_player_respawn_method(self)
            return player_respawn(self.slot)
        end

        function __lua2cs_player_kill_method(self, explode, force)
            return player_kill(self.slot, explode or false, force or false)
        end

        function __lua2cs_player_kick_method(self)
            return player_kick(self.slot)
        end

        function __lua2cs_player_change_team_method(self, team)
            return player_change_team(self.slot, team, false)
        end

        function __lua2cs_player_switch_team_method(self, team)
            return player_change_team(self.slot, team, true)
        end

        function __lua2cs_player_teleport_method(self, position, angles, velocity)
            return player_teleport(self.slot, position, angles, velocity)
        end

        function __lua2cs_player_set_health_method(self, health)
            return player_set_health(self.slot, health)
        end

        function __lua2cs_player_set_armor_method(self, armor)
            return player_set_armor(self.slot, armor)
        end

        function __lua2cs_player_set_money_method(self, money)
            return player_set_money(self.slot, money)
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
        __lua2cs_server_info = nil
        __lua2cs_server_maps = nil
        __lua2cs_server_is_map_valid = nil
        __lua2cs_server_precache_model = nil
        __lua2cs_convar_get = nil
        __lua2cs_convar_set = nil
        __lua2cs_capability_events = nil
        __lua2cs_capability_listeners = nil
        __lua2cs_players_all = nil
        __lua2cs_players_get = nil
        __lua2cs_players_get_userid = nil
        __lua2cs_players_get_steamid = nil
        __lua2cs_players_find = nil
        __lua2cs_players_humans = nil
        __lua2cs_players_bots = nil
        __lua2cs_players_count = nil
        __lua2cs_player_chat = nil
        __lua2cs_player_console = nil
        __lua2cs_player_center = nil
        __lua2cs_player_alert = nil
        __lua2cs_player_html = nil
        __lua2cs_player_refresh = nil
        __lua2cs_player_permission = nil
        __lua2cs_player_can_target = nil
        __lua2cs_player_convar = nil
        __lua2cs_player_execute = nil
        __lua2cs_player_give_item = nil
        __lua2cs_player_remove_item = nil
        __lua2cs_player_remove_weapons = nil
        __lua2cs_player_drop_weapon = nil
        __lua2cs_player_respawn = nil
        __lua2cs_player_kill = nil
        __lua2cs_player_kick = nil
        __lua2cs_player_change_team = nil
        __lua2cs_player_teleport = nil
        __lua2cs_player_set_health = nil
        __lua2cs_player_set_armor = nil
        __lua2cs_player_set_money = nil
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
        Register(state, api, "__lua2cs_server_info", nameof(LuaApi.GetServerInfo));
        Register(state, api, "__lua2cs_server_maps", nameof(LuaApi.GetMapList));
        Register(state, api, "__lua2cs_server_is_map_valid", nameof(LuaApi.ServerIsMapValid));
        Register(state, api, "__lua2cs_server_precache_model", nameof(LuaApi.ServerPrecacheModel));
        Register(state, api, "__lua2cs_convar_get", nameof(LuaApi.GetConVar));
        Register(state, api, "__lua2cs_convar_set", nameof(LuaApi.SetConVar));
        Register(state, api, "__lua2cs_capability_events", nameof(LuaApi.GetEventNames));
        Register(state, api, "__lua2cs_capability_listeners", nameof(LuaApi.GetListenerNames));
        Register(state, api, "__lua2cs_players_all", nameof(LuaApi.GetPlayers));
        Register(state, api, "__lua2cs_players_get", nameof(LuaApi.GetPlayer));
        Register(state, api, "__lua2cs_players_get_userid", nameof(LuaApi.GetPlayerByUserId));
        Register(state, api, "__lua2cs_players_get_steamid", nameof(LuaApi.GetPlayerBySteamId));
        Register(state, api, "__lua2cs_players_find", nameof(LuaApi.FindPlayers));
        Register(state, api, "__lua2cs_players_humans", nameof(LuaApi.GetHumanPlayers));
        Register(state, api, "__lua2cs_players_bots", nameof(LuaApi.GetBots));
        Register(state, api, "__lua2cs_players_count", nameof(LuaApi.GetPlayerCount));
        Register(state, api, "__lua2cs_player_chat", nameof(LuaApi.PlayerPrintChat));
        Register(state, api, "__lua2cs_player_console", nameof(LuaApi.PlayerPrintConsole));
        Register(state, api, "__lua2cs_player_center", nameof(LuaApi.PlayerPrintCenter));
        Register(state, api, "__lua2cs_player_alert", nameof(LuaApi.PlayerPrintAlert));
        Register(state, api, "__lua2cs_player_html", nameof(LuaApi.PlayerPrintHtml));
        Register(state, api, "__lua2cs_player_refresh", nameof(LuaApi.RefreshPlayer));
        Register(state, api, "__lua2cs_player_permission", nameof(LuaApi.PlayerHasPermission));
        Register(state, api, "__lua2cs_player_can_target", nameof(LuaApi.PlayerCanTarget));
        Register(state, api, "__lua2cs_player_convar", nameof(LuaApi.PlayerGetConVar));
        Register(state, api, "__lua2cs_player_execute", nameof(LuaApi.PlayerExecute));
        Register(state, api, "__lua2cs_player_give_item", nameof(LuaApi.PlayerGiveItem));
        Register(state, api, "__lua2cs_player_remove_item", nameof(LuaApi.PlayerRemoveItem));
        Register(state, api, "__lua2cs_player_remove_weapons", nameof(LuaApi.PlayerRemoveWeapons));
        Register(state, api, "__lua2cs_player_drop_weapon", nameof(LuaApi.PlayerDropActiveWeapon));
        Register(state, api, "__lua2cs_player_respawn", nameof(LuaApi.PlayerRespawn));
        Register(state, api, "__lua2cs_player_kill", nameof(LuaApi.PlayerKill));
        Register(state, api, "__lua2cs_player_kick", nameof(LuaApi.PlayerKick));
        Register(state, api, "__lua2cs_player_change_team", nameof(LuaApi.PlayerChangeTeam));
        Register(state, api, "__lua2cs_player_teleport", nameof(LuaApi.PlayerTeleport));
        Register(state, api, "__lua2cs_player_set_health", nameof(LuaApi.PlayerSetHealth));
        Register(state, api, "__lua2cs_player_set_armor", nameof(LuaApi.PlayerSetArmor));
        Register(state, api, "__lua2cs_player_set_money", nameof(LuaApi.PlayerSetMoney));
        Register(state, api, "__lua2cs_command_reply", nameof(LuaApi.CommandReply));
    }

    private static void Register(Lua state, LuaApi api, string luaName, string methodName)
    {
        var method = typeof(LuaApi).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
                     ?? throw new MissingMethodException(typeof(LuaApi).FullName, methodName);
        state.RegisterFunction(luaName, api, method);
    }
}
