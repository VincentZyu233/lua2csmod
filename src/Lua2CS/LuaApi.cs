using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using Microsoft.Extensions.Logging;
using NLua;
using LuaRegistry = KeraLua.LuaRegistry;

namespace Lua2CS;

public sealed class LuaApi
{
    private readonly ILogger _logger;
    private readonly Dictionary<long, CommandInfo> _commandContexts = [];
    private readonly LuaPlugin _plugin;
    private long _nextCommandContextId;
    private LuaFunction? _playerChatMethod;
    private LuaFunction? _playerConsoleMethod;
    private LuaFunction? _playerCenterMethod;
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
        _commandReplyMethod = _plugin.State.GetFunction("__lua2cs_command_reply_method");
        _plugin.State["__lua2cs_player_print_chat"] = null;
        _plugin.State["__lua2cs_player_print_console"] = null;
        _plugin.State["__lua2cs_player_print_center"] = null;
        _plugin.State["__lua2cs_command_reply_method"] = null;
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
        table["slot"] = player.Slot;
        table["name"] = player.PlayerName;
        table["steam_id"] = player.SteamID.ToString(CultureInfo.InvariantCulture);
        table["team"] = player.Team.ToString();
        table["is_bot"] = player.IsBot;
        table["is_hltv"] = player.IsHLTV;
        table["is_alive"] = player.PawnIsAlive;
        table["print_chat"] = _playerChatMethod;
        table["print_console"] = _playerConsoleMethod;
        table["print_center"] = _playerCenterMethod;
        return table;
    }

    internal static bool IsUsablePlayer(CCSPlayerController? player) => player is { IsValid: true };

    private CCSPlayerController? ResolvePlayer(long slot)
    {
        if (slot is < 0 or > 255) return null;
        var player = Utilities.GetPlayerFromSlot((int)slot);
        return IsUsablePlayer(player) ? player : null;
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
