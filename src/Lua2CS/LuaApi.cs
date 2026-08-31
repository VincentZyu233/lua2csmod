using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.ValveConstants.Protobuf;
using Lua2CS.Bindings;
using Microsoft.Extensions.Logging;
using NLua;
using LuaRegistry = KeraLua.LuaRegistry;

namespace Lua2CS;

public sealed class LuaApi
{
    private static readonly string[] PlayerMethodNames =
    [
        "__lua2cs_player_print_chat",
        "__lua2cs_player_print_console",
        "__lua2cs_player_print_center",
        "__lua2cs_player_print_alert",
        "__lua2cs_player_print_html",
        "__lua2cs_player_refresh_method",
        "__lua2cs_player_has_permission_method",
        "__lua2cs_player_can_target_method",
        "__lua2cs_player_get_convar_method",
        "__lua2cs_player_execute_method",
        "__lua2cs_player_execute_server_method",
        "__lua2cs_player_give_item_method",
        "__lua2cs_player_remove_item_method",
        "__lua2cs_player_remove_weapons_method",
        "__lua2cs_player_drop_weapon_method",
        "__lua2cs_player_respawn_method",
        "__lua2cs_player_kill_method",
        "__lua2cs_player_kick_method",
        "__lua2cs_player_change_team_method",
        "__lua2cs_player_switch_team_method",
        "__lua2cs_player_teleport_method",
        "__lua2cs_player_set_health_method",
        "__lua2cs_player_set_armor_method",
        "__lua2cs_player_set_money_method",
        "__lua2cs_command_reply_method"
    ];

    private readonly ILogger _logger;
    private readonly Dictionary<long, CommandInfo> _commandContexts = [];
    private readonly LuaPlugin _plugin;
    private long _nextCommandContextId;
    private LuaFunction? _playerChatMethod;
    private LuaFunction? _playerConsoleMethod;
    private LuaFunction? _playerCenterMethod;
    private LuaFunction? _playerAlertMethod;
    private LuaFunction? _playerHtmlMethod;
    private LuaFunction? _playerRefreshMethod;
    private LuaFunction? _playerPermissionMethod;
    private LuaFunction? _playerCanTargetMethod;
    private LuaFunction? _playerConVarMethod;
    private LuaFunction? _playerExecuteMethod;
    private LuaFunction? _playerExecuteServerMethod;
    private LuaFunction? _playerGiveItemMethod;
    private LuaFunction? _playerRemoveItemMethod;
    private LuaFunction? _playerRemoveWeaponsMethod;
    private LuaFunction? _playerDropWeaponMethod;
    private LuaFunction? _playerRespawnMethod;
    private LuaFunction? _playerKillMethod;
    private LuaFunction? _playerKickMethod;
    private LuaFunction? _playerChangeTeamMethod;
    private LuaFunction? _playerSwitchTeamMethod;
    private LuaFunction? _playerTeleportMethod;
    private LuaFunction? _playerSetHealthMethod;
    private LuaFunction? _playerSetArmorMethod;
    private LuaFunction? _playerSetMoneyMethod;
    private LuaFunction? _commandReplyMethod;

    internal LuaApi(LuaPlugin plugin, ILogger logger)
    {
        _plugin = plugin;
        _logger = logger;
    }

    internal void InitializeLuaMethods()
    {
        _playerChatMethod = _plugin.State.GetFunction("__lua2cs_player_print_chat");
        _playerConsoleMethod = _plugin.State.GetFunction("__lua2cs_player_print_console");
        _playerCenterMethod = _plugin.State.GetFunction("__lua2cs_player_print_center");
        _playerAlertMethod = _plugin.State.GetFunction("__lua2cs_player_print_alert");
        _playerHtmlMethod = _plugin.State.GetFunction("__lua2cs_player_print_html");
        _playerRefreshMethod = _plugin.State.GetFunction("__lua2cs_player_refresh_method");
        _playerPermissionMethod = _plugin.State.GetFunction("__lua2cs_player_has_permission_method");
        _playerCanTargetMethod = _plugin.State.GetFunction("__lua2cs_player_can_target_method");
        _playerConVarMethod = _plugin.State.GetFunction("__lua2cs_player_get_convar_method");
        _playerExecuteMethod = _plugin.State.GetFunction("__lua2cs_player_execute_method");
        _playerExecuteServerMethod = _plugin.State.GetFunction("__lua2cs_player_execute_server_method");
        _playerGiveItemMethod = _plugin.State.GetFunction("__lua2cs_player_give_item_method");
        _playerRemoveItemMethod = _plugin.State.GetFunction("__lua2cs_player_remove_item_method");
        _playerRemoveWeaponsMethod = _plugin.State.GetFunction("__lua2cs_player_remove_weapons_method");
        _playerDropWeaponMethod = _plugin.State.GetFunction("__lua2cs_player_drop_weapon_method");
        _playerRespawnMethod = _plugin.State.GetFunction("__lua2cs_player_respawn_method");
        _playerKillMethod = _plugin.State.GetFunction("__lua2cs_player_kill_method");
        _playerKickMethod = _plugin.State.GetFunction("__lua2cs_player_kick_method");
        _playerChangeTeamMethod = _plugin.State.GetFunction("__lua2cs_player_change_team_method");
        _playerSwitchTeamMethod = _plugin.State.GetFunction("__lua2cs_player_switch_team_method");
        _playerTeleportMethod = _plugin.State.GetFunction("__lua2cs_player_teleport_method");
        _playerSetHealthMethod = _plugin.State.GetFunction("__lua2cs_player_set_health_method");
        _playerSetArmorMethod = _plugin.State.GetFunction("__lua2cs_player_set_armor_method");
        _playerSetMoneyMethod = _plugin.State.GetFunction("__lua2cs_player_set_money_method");
        _commandReplyMethod = _plugin.State.GetFunction("__lua2cs_command_reply_method");

        foreach (var name in PlayerMethodNames)
        {
            _plugin.State[name] = null;
        }
    }

    public void CreatePlugin(LuaTable spec)
    {
        if (!string.IsNullOrEmpty(_plugin.Name))
        {
            throw new InvalidOperationException("cs.plugin may only be called once per script.");
        }

        _plugin.Name = ReadString(spec, "name", required: true);
        _plugin.Version = ReadString(spec, "version", defaultValue: "0.0.0");
        _plugin.Description = ReadString(spec, "description", defaultValue: string.Empty);
    }

    public long RegisterEvent(string eventName, LuaFunction callback, LuaTable options)
    {
        var mode = ReadString(options, "mode", defaultValue: "post").Equals("pre", StringComparison.OrdinalIgnoreCase)
            ? HookMode.Pre
            : HookMode.Post;
        var id = _plugin.NextRegistrationId();
        _plugin.AddRegistration(new EventRegistration(id, eventName.Trim(), mode, callback));
        return id;
    }

    public long RegisterListener(string listenerName, LuaFunction callback)
    {
        var id = _plugin.NextRegistrationId();
        _plugin.AddRegistration(new ListenerRegistration(id, listenerName.Trim(), callback));
        return id;
    }

    public long RegisterCommand(string name, LuaTable options, LuaFunction callback)
    {
        var id = _plugin.NextRegistrationId();
        _plugin.AddRegistration(new CommandRegistration(
            id,
            name.Trim(),
            ReadString(options, "description", defaultValue: "Lua 命令"),
            ReadString(options, "permission", defaultValue: string.Empty),
            ReadBool(options, "allow_console", true),
            ReadInt(options, "min_args", 0),
            ReadString(options, "usage", defaultValue: string.Empty),
            callback));
        return id;
    }

    public long RegisterTimer(double interval, LuaFunction callback, LuaTable options)
    {
        if (!double.IsFinite(interval) || interval <= 0 || interval > float.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "Timer interval must be greater than zero.");
        }

        var id = _plugin.NextRegistrationId();
        _plugin.AddRegistration(new TimerRegistration(
            id,
            (float)interval,
            ReadBool(options, "repeating", ReadBool(options, "repeat", false)),
            ReadBool(options, "stop_on_map_change", true),
            callback));
        return id;
    }

    public void RegisterLoad(LuaFunction callback) => _plugin.LoadCallback = callback;
    public void RegisterUnload(LuaFunction callback) => _plugin.UnloadCallback = callback;
    public bool CancelRegistration(long registrationId) => _plugin.RemoveRegistration(registrationId);

    public void LogDebug(object? message) => _logger.LogDebug("[{Plugin}] {Message}", _plugin.Name, message);
    public void LogInfo(object? message) => _logger.LogInformation("[{Plugin}] {Message}", _plugin.Name, message);
    public void LogWarning(object? message) => _logger.LogWarning("[{Plugin}] {Message}", _plugin.Name, message);
    public void LogError(object? message) => _logger.LogError("[{Plugin}] {Message}", _plugin.Name, message);

    public void ServerPrintChatAll(string message) => Server.PrintToChatAll(message);
    public void ServerPrintConsole(string message) => Server.PrintToConsole(message);
    public void ServerExecute(string command) => Server.ExecuteCommand(command);

    public LuaTable GetServerInfo()
    {
        var table = NewTable();
        table["map_name"] = Server.MapName;
        table["max_players"] = Server.MaxPlayers;
        table["tick_interval"] = Server.TickInterval;
        table["tick_count"] = Server.TickCount;
        table["current_time"] = Server.CurrentTime;
        table["ticked_time"] = Server.TickedTime;
        table["engine_time"] = Server.EngineTime;
        table["frame_time"] = Server.FrameTime;
        return table;
    }

    public LuaTable GetMapList()
    {
        var table = NewTable();
        var index = 1;
        foreach (var map in Server.GetMapList()) table[index++] = map;
        return table;
    }

    public bool ServerIsMapValid(string mapName) => Server.IsMapValid(mapName);
    public void ServerPrecacheModel(string modelName) => Server.PrecacheModel(modelName);

    public string? GetConVar(string name) => ConVar.Find(name.Trim())?.StringValue;

    public bool SetConVar(string name, object? value)
    {
        var conVar = ConVar.Find(name.Trim());
        if (conVar is null) return false;
        conVar.StringValue = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        return true;
    }

    public LuaTable GetEventNames() => CreateStringList(EventBindings.Names.Order(StringComparer.OrdinalIgnoreCase));
    public LuaTable GetListenerNames() => CreateStringList(ListenerBindings.Names.Order(StringComparer.OrdinalIgnoreCase));

    public LuaTable GetPlayers()
    {
        var table = NewTable();
        var index = 1;
        foreach (var player in Utilities.GetPlayers().Where(IsUsablePlayer))
        {
            using var playerTable = CreatePlayerTable(player);
            table[index++] = playerTable;
        }

        return table;
    }

    public LuaTable? GetPlayer(long slot)
    {
        var player = ResolvePlayer(slot);
        return player is null ? null : CreatePlayerTable(player);
    }

    public LuaTable? GetPlayerByUserId(long userId)
    {
        if (userId is < 0 or > int.MaxValue) return null;
        var player = Utilities.GetPlayerFromUserid((int)userId);
        return IsUsablePlayer(player) ? CreatePlayerTable(player!) : null;
    }

    public LuaTable? GetPlayerBySteamId(string steamId)
    {
        if (!ulong.TryParse(steamId.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var id)) return null;
        var player = Utilities.GetPlayerFromSteamId64(id);
        return IsUsablePlayer(player) ? CreatePlayerTable(player!) : null;
    }

    public LuaTable FindPlayers(string query)
    {
        query = query.Trim();
        if (string.IsNullOrEmpty(query)) return NewTable();

        IEnumerable<CCSPlayerController> players = Utilities.GetPlayers().Where(IsUsablePlayer);
        if (int.TryParse(query, NumberStyles.None, CultureInfo.InvariantCulture, out var numeric))
        {
            players = players.Where(player => player.Slot == numeric || player.UserId == numeric);
        }
        else if (ulong.TryParse(query, NumberStyles.None, CultureInfo.InvariantCulture, out var steamId))
        {
            players = players.Where(player => player.SteamID == steamId);
        }
        else
        {
            players = players.Where(player => player.PlayerName.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return CreatePlayerList(players);
    }

    public LuaTable GetHumanPlayers() => CreatePlayerList(Utilities.GetPlayers().Where(player => IsUsablePlayer(player) && !player.IsBot && !player.IsHLTV));
    public LuaTable GetBots() => CreatePlayerList(Utilities.GetPlayers().Where(player => IsUsablePlayer(player) && player.IsBot));
    public long GetPlayerCount() => Utilities.GetPlayers().LongCount(IsUsablePlayer);

    public LuaTable? RefreshPlayer(long slot) => GetPlayer(slot);

    public bool PlayerPrintChat(long slot, string message)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        player.PrintToChat(message);
        return true;
    }

    public bool PlayerPrintConsole(long slot, string message)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        player.PrintToConsole(message);
        return true;
    }

    public bool PlayerPrintCenter(long slot, string message)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        player.PrintToCenter(message);
        return true;
    }

    public bool PlayerPrintAlert(long slot, string message)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        player.PrintToCenterAlert(message);
        return true;
    }

    public bool PlayerPrintHtml(long slot, string message, long duration)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        player.PrintToCenterHtml(message, (int)Math.Clamp(duration, 1, 60));
        return true;
    }

    public bool PlayerHasPermission(long slot, string permission)
    {
        var player = ResolvePlayer(slot);
        return player is not null && AdminManager.PlayerHasPermissions(player, permission);
    }

    public bool PlayerCanTarget(long slot, long targetSlot)
    {
        var player = ResolvePlayer(slot);
        var target = ResolvePlayer(targetSlot);
        return player is not null && target is not null && AdminManager.CanPlayerTarget(player, target);
    }

    public string? PlayerGetConVar(long slot, string name) => ResolvePlayer(slot)?.GetConVarValue(name);

    public bool PlayerExecute(long slot, string command, bool asServer)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        if (asServer) player.ExecuteClientCommandFromServer(command);
        else player.ExecuteClientCommand(command);
        return true;
    }

    public bool PlayerGiveItem(long slot, string designerName)
    {
        var player = ResolvePlayer(slot);
        if (player is null || string.IsNullOrWhiteSpace(designerName)) return false;
        return player.GiveNamedItem(designerName.Trim()) != IntPtr.Zero;
    }

    public bool PlayerRemoveItem(long slot, string designerName)
    {
        var player = ResolvePlayer(slot);
        return player is not null && !string.IsNullOrWhiteSpace(designerName)
                                  && player.RemoveItemByDesignerName(designerName.Trim());
    }

    public bool PlayerRemoveWeapons(long slot)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        player.RemoveWeapons();
        return true;
    }

    public bool PlayerDropActiveWeapon(long slot)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        player.DropActiveWeapon();
        return true;
    }

    public bool PlayerRespawn(long slot)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        player.Respawn();
        return true;
    }

    public bool PlayerKill(long slot, bool explode, bool force)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        player.CommitSuicide(explode, force);
        return true;
    }

    public bool PlayerKick(long slot)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        player.Disconnect(NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED);
        return true;
    }

    public bool PlayerChangeTeam(long slot, object? team, bool keepAlive)
    {
        var player = ResolvePlayer(slot);
        if (player is null) return false;
        var resolvedTeam = ParseTeam(team);
        if (keepAlive) player.SwitchTeam(resolvedTeam);
        else player.ChangeTeam(resolvedTeam);
        return true;
    }

    public bool PlayerTeleport(long slot, LuaTable? position, LuaTable? angles, LuaTable? velocity)
    {
        var pawn = ResolvePlayer(slot)?.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid) return false;
        var parsedPosition = ReadVector(position);
        var parsedAngles = ReadVector(angles);
        var parsedVelocity = ReadVector(velocity);
        if (parsedPosition is null && parsedAngles is null && parsedVelocity is null) return false;
        pawn.Teleport(parsedPosition, parsedAngles, parsedVelocity);
        return true;
    }

    public bool PlayerSetHealth(long slot, long health)
    {
        var pawn = ResolvePlayer(slot)?.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid) return false;
        pawn.Health = (int)Math.Clamp(health, 0, int.MaxValue);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        return true;
    }

    public bool PlayerSetArmor(long slot, long armor)
    {
        var pawn = ResolvePlayer(slot)?.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid) return false;
        pawn.ArmorValue = (int)Math.Clamp(armor, 0, int.MaxValue);
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
        return true;
    }

    public bool PlayerSetMoney(long slot, long money)
    {
        var services = ResolvePlayer(slot)?.InGameMoneyServices;
        if (services is null) return false;
        services.Account = (int)Math.Clamp(money, 0, int.MaxValue);
        return true;
    }

    public bool CommandReply(long contextId, string message)
    {
        if (!_commandContexts.TryGetValue(contextId, out var command)) return false;
        command.ReplyToCommand(message);
        return true;
    }

    internal LuaEventSnapshot CreateEventSnapshot(GameEvent gameEvent, GameEventInfo info, bool writable)
    {
        var eventTable = NewTable();
        var infoTable = NewTable();
        var properties = EventProperties(gameEvent.GetType()).ToArray();

        eventTable["name"] = gameEvent.EventName;
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(gameEvent);
                SetMappedValue(eventTable, ToSnakeCase(property.Name), value);
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Unable to read event field {Field}", property.Name);
            }
        }

        var existingPlayer = eventTable["player"];
        var userIdValue = eventTable["userid"];
        if (existingPlayer is null && userIdValue is LuaTable userTable)
        {
            eventTable["player"] = userTable;
            userTable.Dispose();
        }
        else if (existingPlayer is null && userIdValue is long userId)
        {
            var player = Utilities.GetPlayerFromUserid((int)userId);
            if (IsUsablePlayer(player))
            {
                using var playerTable = CreatePlayerTable(player!);
                eventTable["player"] = playerTable;
            }
        }

        infoTable["dont_broadcast"] = info.DontBroadcast;
        return new LuaEventSnapshot(this, gameEvent, info, eventTable, infoTable, properties, writable);
    }

    internal LuaMappedArguments MapArguments(IEnumerable<object?> values)
    {
        var mapped = new List<object?>();
        var owned = new List<IDisposable>();
        foreach (var value in values)
        {
            if (value is CCSPlayerController player)
            {
                var table = CreatePlayerTable(player);
                owned.Add(table);
                mapped.Add(table);
            }
            else if (value is SteamID steamId)
            {
                mapped.Add(steamId.SteamId64.ToString(CultureInfo.InvariantCulture));
            }
            else if (value is Vector vector)
            {
                var table = CreateVectorTable(vector.X, vector.Y, vector.Z);
                owned.Add(table);
                mapped.Add(table);
            }
            else if (value is QAngle angle)
            {
                var table = CreateVectorTable(angle.X, angle.Y, angle.Z);
                owned.Add(table);
                mapped.Add(table);
            }
            else if (value?.GetType().IsEnum == true)
            {
                mapped.Add(value.ToString());
            }
            else if (IsLuaPrimitive(value))
            {
                mapped.Add(value);
            }
            else
            {
                mapped.Add(value?.ToString());
            }
        }

        return new LuaMappedArguments(mapped.ToArray(), owned);
    }

    internal LuaCommandSnapshot CreateCommandSnapshot(CommandInfo command)
    {
        var id = Interlocked.Increment(ref _nextCommandContextId);
        _commandContexts[id] = command;
        var table = NewTable();
        var args = NewTable();

        for (var index = 1; index < command.ArgCount; index++)
        {
            args[index] = command.GetArg(index);
        }

        table["__context_id"] = id;
        table["name"] = command.ArgCount > 0 ? command.GetArg(0) : string.Empty;
        table["args"] = args;
        table["arg_string"] = command.ArgString;
        table["context"] = command.CallingContext.ToString().ToLowerInvariant();
        table["reply"] = _commandReplyMethod;
        args.Dispose();
        return new LuaCommandSnapshot(table, () => _commandContexts.Remove(id));
    }

    internal HookResult ParseHookResult(object? value)
    {
        if (value is long number && Enum.IsDefined(typeof(HookResult), (int)number))
        {
            return (HookResult)(int)number;
        }

        return value?.ToString()?.Trim().ToLowerInvariant() switch
        {
            "changed" => HookResult.Changed,
            "handled" => HookResult.Handled,
            "stop" => HookResult.Stop,
            _ => HookResult.Continue
        };
    }

    internal LuaTable CreatePlayerTable(CCSPlayerController player)
    {
        var table = NewTable();
        var pawn = player.PlayerPawn.Value is { IsValid: true } validPawn ? validPawn : null;
        var money = player.InGameMoneyServices?.Account;
        table["slot"] = player.Slot;
        table["user_id"] = player.UserId;
        table["name"] = player.PlayerName;
        table["steam_id"] = player.SteamID.ToString(CultureInfo.InvariantCulture);
        table["ip_address"] = player.IpAddress;
        table["team"] = player.Team.ToString();
        table["team_id"] = (long)player.Team;
        table["is_bot"] = player.IsBot;
        table["is_hltv"] = player.IsHLTV;
        table["is_alive"] = player.PawnIsAlive;
        table["ping"] = player.Ping;
        table["score"] = player.Score;
        table["round_score"] = player.RoundScore;
        table["mvps"] = player.MVPs;
        table["health"] = pawn?.Health;
        table["armor"] = pawn?.ArmorValue;
        table["money"] = money;
        table["has_helmet"] = player.PawnHasHelmet;
        table["has_defuser"] = player.PawnHasDefuser;
        table["in_buy_zone"] = pawn?.InBuyZone;
        table["in_bomb_zone"] = pawn?.InBombZone;
        table["buttons"] = ReadButtons(pawn);
        table["active_weapon"] = pawn?.WeaponServices?.ActiveWeapon.Value?.DesignerName;

        if (pawn?.AbsOrigin is { } position)
        {
            using var positionTable = CreateVectorTable(position.X, position.Y, position.Z);
            table["position"] = positionTable;
        }
        if (pawn is not null)
        {
            using var velocityTable = CreateVectorTable(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z);
            using var anglesTable = CreateVectorTable(pawn.EyeAngles.X, pawn.EyeAngles.Y, pawn.EyeAngles.Z);
            table["velocity"] = velocityTable;
            table["eye_angles"] = anglesTable;
        }

        var weapons = NewTable();
        var weaponIndex = 1;
        if (pawn?.WeaponServices is { } weaponServices)
        {
            foreach (var weapon in weaponServices.MyWeapons.Select(handle => handle.Value).Where(weapon => weapon is { IsValid: true }))
            {
                weapons[weaponIndex++] = weapon!.DesignerName;
            }
        }
        table["weapons"] = weapons;
        weapons.Dispose();

        table["print_chat"] = _playerChatMethod;
        table["print_console"] = _playerConsoleMethod;
        table["print_center"] = _playerCenterMethod;
        table["print_alert"] = _playerAlertMethod;
        table["print_html"] = _playerHtmlMethod;
        table["refresh"] = _playerRefreshMethod;
        table["has_permission"] = _playerPermissionMethod;
        table["can_target"] = _playerCanTargetMethod;
        table["get_convar"] = _playerConVarMethod;
        table["execute"] = _playerExecuteMethod;
        table["execute_as_server"] = _playerExecuteServerMethod;
        table["give_item"] = _playerGiveItemMethod;
        table["remove_item"] = _playerRemoveItemMethod;
        table["remove_weapons"] = _playerRemoveWeaponsMethod;
        table["drop_active_weapon"] = _playerDropWeaponMethod;
        table["respawn"] = _playerRespawnMethod;
        table["kill"] = _playerKillMethod;
        table["kick"] = _playerKickMethod;
        table["change_team"] = _playerChangeTeamMethod;
        table["switch_team"] = _playerSwitchTeamMethod;
        table["teleport"] = _playerTeleportMethod;
        table["set_health"] = _playerSetHealthMethod;
        table["set_armor"] = _playerSetArmorMethod;
        table["set_money"] = _playerSetMoneyMethod;
        return table;
    }

    internal static bool IsUsablePlayer(CCSPlayerController? player) => player is { IsValid: true };

    private CCSPlayerController? ResolvePlayer(long slot)
    {
        if (slot is < 0 or > 255) return null;
        var player = Utilities.GetPlayerFromSlot((int)slot);
        return IsUsablePlayer(player) ? player : null;
    }

    private LuaTable CreatePlayerList(IEnumerable<CCSPlayerController> players)
    {
        var table = NewTable();
        var index = 1;
        foreach (var player in players)
        {
            using var playerTable = CreatePlayerTable(player);
            table[index++] = playerTable;
        }
        return table;
    }

    private static long ReadButtons(CCSPlayerPawn? pawn)
    {
        if (pawn?.MovementServices is null) return 0;
        return unchecked((long)pawn.MovementServices.Buttons.ButtonStates[0]);
    }

    private LuaTable CreateStringList(IEnumerable<string> values)
    {
        var table = NewTable();
        var index = 1;
        foreach (var value in values) table[index++] = value;
        return table;
    }

    private LuaTable CreateVectorTable(float x, float y, float z)
    {
        var table = NewTable();
        table["x"] = x;
        table["y"] = y;
        table["z"] = z;
        table[1] = x;
        table[2] = y;
        table[3] = z;
        return table;
    }

    private static System.Numerics.Vector3? ReadVector(LuaTable? table)
    {
        if (table is null) return null;
        var x = ReadNumber(table["x"] ?? table[1], "x");
        var y = ReadNumber(table["y"] ?? table[2], "y");
        var z = ReadNumber(table["z"] ?? table[3], "z");
        return new System.Numerics.Vector3(x, y, z);
    }

    private static float ReadNumber(object? value, string field)
    {
        if (value is null) throw new InvalidDataException($"向量缺少 {field} 分量。");
        var number = Convert.ToSingle(value, CultureInfo.InvariantCulture);
        if (!float.IsFinite(number)) throw new InvalidDataException($"向量的 {field} 分量必须是有限数值。");
        return number;
    }

    internal static CsTeam ParseTeam(object? value)
    {
        if (value is long or int or double)
        {
            var number = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (number is >= 0 and <= 3 && Enum.IsDefined(typeof(CsTeam), (byte)number)) return (CsTeam)number;
        }

        return value?.ToString()?.Trim().ToLowerInvariant() switch
        {
            "0" or "none" => CsTeam.None,
            "1" or "spec" or "spectator" => CsTeam.Spectator,
            "2" or "t" or "terrorist" => CsTeam.Terrorist,
            "3" or "ct" or "counterterrorist" or "counter_terrorist" => CsTeam.CounterTerrorist,
            _ => throw new ArgumentException("队伍必须是 none、spectator、t、ct 或 0 到 3。", nameof(value))
        };
    }

    private LuaTable NewTable()
    {
        var state = _plugin.State.State;
        state.NewTable();
        var reference = state.Ref(LuaRegistry.Index);
        return new LuaTable(reference, _plugin.State);
    }

    private void SetMappedValue(LuaTable table, string key, object? value)
    {
        if (value is CCSPlayerController player && IsUsablePlayer(player))
        {
            using var playerTable = CreatePlayerTable(player);
            table[key] = playerTable;
        }
        else if (value is SteamID steamId)
        {
            table[key] = steamId.SteamId64.ToString(CultureInfo.InvariantCulture);
        }
        else if (value is Vector vector)
        {
            using var vectorTable = CreateVectorTable(vector.X, vector.Y, vector.Z);
            table[key] = vectorTable;
        }
        else if (value is QAngle angle)
        {
            using var angleTable = CreateVectorTable(angle.X, angle.Y, angle.Z);
            table[key] = angleTable;
        }
        else if (value is ulong unsigned)
        {
            table[key] = unsigned.ToString(CultureInfo.InvariantCulture);
        }
        else if (value?.GetType().IsEnum == true)
        {
            table[key] = value.ToString();
        }
        else if (IsLuaPrimitive(value))
        {
            table[key] = value;
        }
    }

    private static IEnumerable<PropertyInfo> EventProperties(Type eventType) => eventType
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.DeclaringType != typeof(GameEvent)
                           && property.DeclaringType != typeof(NativeObject)
                           && property.GetIndexParameters().Length == 0
                           && property.CanRead);

    private static bool IsLuaPrimitive(object? value) => value is null or string or bool
        or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    internal static object? ConvertLuaValue(object? value, Type targetType)
    {
        var nullableType = Nullable.GetUnderlyingType(targetType);
        targetType = nullableType ?? targetType;
        if (value is null) return nullableType is not null || !targetType.IsValueType ? null : Activator.CreateInstance(targetType);
        if (targetType == typeof(string)) return value.ToString();
        if (targetType == typeof(bool)) return value is bool boolean ? boolean : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        if (targetType == typeof(ulong)) return ulong.Parse(value.ToString()!, CultureInfo.InvariantCulture);
        if (targetType == typeof(Vector) && value is LuaTable vectorTable)
        {
            var vector = ReadVector(vectorTable)!.Value;
            return new Vector(vector.X, vector.Y, vector.Z);
        }
        if (targetType == typeof(QAngle) && value is LuaTable angleTable)
        {
            var angle = ReadVector(angleTable)!.Value;
            return new QAngle(angle.X, angle.Y, angle.Z);
        }
        if (targetType.IsEnum) return Enum.Parse(targetType, value.ToString()!, true);
        if (typeof(CCSPlayerController).IsAssignableFrom(targetType) && value is LuaTable playerTable)
        {
            var slot = Convert.ToInt32(playerTable["slot"], CultureInfo.InvariantCulture);
            return Utilities.GetPlayerFromSlot(slot);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    internal static string ToSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var result = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0 && (char.IsLower(value[index - 1]) || index + 1 < value.Length && char.IsLower(value[index + 1])))
            {
                result.Append('_');
            }
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }

    private static string ReadString(LuaTable table, string key, bool required = false, string defaultValue = "")
    {
        var value = table[key]?.ToString()?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            if (required) throw new InvalidDataException($"Lua plugin field '{key}' is required.");
            return defaultValue;
        }
        return value;
    }

    private static bool ReadBool(LuaTable table, string key, bool defaultValue) => table[key] is bool value ? value : defaultValue;

    private static int ReadInt(LuaTable table, string key, int defaultValue)
    {
        var value = table[key];
        return value is null ? defaultValue : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    internal sealed class LuaEventSnapshot(
        LuaApi api,
        GameEvent gameEvent,
        GameEventInfo info,
        LuaTable eventTable,
        LuaTable infoTable,
        PropertyInfo[] properties,
        bool writable) : IDisposable
    {
        public LuaTable Event => eventTable;
        public LuaTable Info => infoTable;

        public void Apply()
        {
            if (!writable) return;
            foreach (var property in properties.Where(item => item.CanWrite))
            {
                var key = ToSnakeCase(property.Name);
                try
                {
                    property.SetValue(gameEvent, ConvertLuaValue(eventTable[key], property.PropertyType));
                }
                catch (Exception exception)
                {
                    api._logger.LogWarning(exception, "Unable to write Lua event field {Field}", key);
                }
            }

            if (infoTable["dont_broadcast"] is bool dontBroadcast)
            {
                info.DontBroadcast = dontBroadcast;
            }
        }

        public void Dispose()
        {
            eventTable.Dispose();
            infoTable.Dispose();
        }
    }

    internal sealed class LuaMappedArguments(object?[] values, List<IDisposable> owned) : IDisposable
    {
        public object?[] Values { get; } = values;
        public void Dispose()
        {
            foreach (var item in owned) item.Dispose();
        }
    }

    internal sealed class LuaCommandSnapshot(LuaTable table, Action dispose) : IDisposable
    {
        public LuaTable Table { get; } = table;
        public void Dispose()
        {
            dispose();
            Table.Dispose();
        }
    }
}
