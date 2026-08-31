using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CssTimer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Lua2CS.Bindings;

public sealed class TimerBindings(BasePlugin host)
{
    public void Validate(TimerRegistration registration)
    {
        if (!float.IsFinite(registration.Interval) || registration.Interval <= 0)
        {
            throw new InvalidDataException("Lua timer interval must be greater than zero.");
        }
    }

    public IRegistrationHandle Activate(LuaPlugin plugin, TimerRegistration registration)
    {
        var flags = (TimerFlags)0;
        if (registration.Repeat) flags |= TimerFlags.REPEAT;
        if (registration.StopOnMapChange) flags |= TimerFlags.STOP_ON_MAPCHANGE;

        CssTimer? timer = null;
        timer = host.AddTimer(registration.Interval, () =>
        {
            if (!plugin.IsActive) return;
            plugin.Invoke(registration.Callback);
            if (!registration.Repeat && timer is not null)
            {
                host.Timers.Remove(timer);
            }
        }, flags);

        return new RegistrationHandle(registration.Id, () =>
        {
            timer.Kill();
            host.Timers.Remove(timer);
        });
    }
}
