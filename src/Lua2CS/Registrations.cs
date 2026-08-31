using CounterStrikeSharp.API.Core;
using NLua;

namespace Lua2CS;

public abstract record RegistrationDefinition(long Id);

public sealed record EventRegistration(
    long Id,
    string EventName,
    HookMode Mode,
    LuaFunction Callback) : RegistrationDefinition(Id);

public sealed record ListenerRegistration(
    long Id,
    string ListenerName,
    LuaFunction Callback) : RegistrationDefinition(Id);

public sealed record CommandRegistration(
    long Id,
    string Name,
    string Description,
    string Permission,
    bool AllowConsole,
    int MinArgs,
    string Usage,
    LuaFunction Callback) : RegistrationDefinition(Id);

public sealed record TimerRegistration(
    long Id,
    float Interval,
    bool Repeat,
    bool StopOnMapChange,
    LuaFunction Callback) : RegistrationDefinition(Id);

public interface IRegistrationHandle : IDisposable
{
    long Id { get; }
}

internal sealed class RegistrationHandle(long id, Action dispose) : IRegistrationHandle
{
    private Action? _dispose = dispose;

    public long Id { get; } = id;

    public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
}
