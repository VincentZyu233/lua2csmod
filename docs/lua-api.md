# Lua scripting API

## Plugin metadata and lifecycle

Every top-level script must call `cs.plugin` exactly once:

```lua
local plugin = cs.plugin({
    name = "Required name",
    version = "1.0.0",
    description = "Optional description"
})

plugin:on_load(function(hot_reload) end)
plugin:on_unload(function(hot_reload) end)
```

Returning `false` from a lifecycle callback rejects the activation. An exception during preparation or activation also rejects it.

## Game events

```lua
local id = plugin:on("player_death", function(event, info)
    if event.attacker ~= nil then
        event.attacker:print_chat("Kill registered")
    end
    return cs.continue
end, { mode = "post" })
```

Event names are the CounterStrikeSharp names such as `player_chat`, `round_start`, and `player_death`. `mode` is `pre` or `post`; the default is `post`.

Generated C# properties are exposed as snake-case Lua fields. For example, `DmgArmor` becomes `dmg_armor`. Player-valued fields are player tables. `event.player` is also supplied when the event has a resolvable `userid` field.

Pre callbacks may update event fields and `info.dont_broadcast`. Return one of `cs.continue`, `cs.changed`, `cs.handled`, or `cs.stop`.

## Listeners

```lua
plugin:listen("OnMapStart", function(map_name)
    cs.log.info("Map started: " .. map_name)
end)
```

Listener names match CounterStrikeSharp's `Listeners` delegates. Primitive arguments and players are converted to Lua values; unsupported native objects are represented by their string form. Listeners returning a hook result use the same constants as events.

## Commands

```lua
plugin:command("css_greet", {
    description = "Greet a player",
    permission = "@css/generic",
    allow_console = true,
    min_args = 1,
    usage = "<name>"
}, function(player, command)
    command:reply("Hello " .. command.args[1])
end)
```

`player` is `nil` for server console/RCON. A command object has `name`, `args`, `arg_string`, `context`, and `command:reply(message)`.

The options table may be omitted when no options are needed.

## Timers

```lua
local timer_id = plugin:timer(5, function()
    cs.log.info("Five seconds elapsed")
end, {
    repeating = true,
    stop_on_map_change = true
})

plugin:cancel(timer_id)
```

All registrations return an ID accepted by `plugin:cancel`. Every remaining registration is removed automatically when the script unloads or reloads.

## Server API

```lua
cs.server.print_chat_all("Message")
cs.server.print_console("Message")
cs.server.execute("sv_cheats 0")

cs.log.debug("message")
cs.log.info("message")
cs.log.warn("message")
cs.log.error("message")
```

Chat control codes are available in `cs.colors`, including `default`, `green`, `lime`, `red`, `yellow`, `blue`, `purple`, `grey`, `gold`, and `orange`.

## Players

```lua
for _, player in ipairs(cs.players.all()) do
    player:print_chat("Hello " .. player.name)
end

local player = cs.players.get(0)
```

Player fields are `slot`, `name`, `steam_id`, `team`, `is_bot`, `is_hltv`, and `is_alive`. Methods are `print_chat`, `print_console`, and `print_center`. A player table is a short-lived view; resolve it again instead of retaining it across disconnects or map changes.

## Modules

Scripts may load sibling modules using `require("module")` or `require("folder.module")`. Top-level files beginning with `_` and files inside subdirectories are not loaded as independent plugins.
