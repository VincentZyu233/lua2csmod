# Lua2CS

Lua2CS is a Lua 5.4 plugin host for CounterStrikeSharp. The C# host is installed once; gameplay scripts can then be loaded and reloaded without compiling a DLL or restarting the CS2 server.

## Requirements

- Linux x64 CS2 dedicated server
- Metamod:Source
- CounterStrikeSharp API 1.0.373 or newer, with the .NET 10 runtime

## Installation

1. Download `Lua2CS-preview-linux-x64.zip` from the prerelease.
2. Extract it into the server's `game/csgo` directory.
3. Put Lua scripts in `addons/counterstrikesharp/plugins/Lua2CS/scripts`.
4. Restart the server or run `css_plugins load Lua2CS`.

The archive includes NLua, KeraLua and the Linux x64 Lua 5.4 native library. No system Lua installation is required.

## Script example

```lua
local plugin = cs.plugin({
    name = "Hello Lua",
    version = "1.0.0"
})

plugin:on("player_chat", function(event)
    if event.player ~= nil and event.text:lower() == "hello" then
        event.player:print_chat("Hello from Lua 5.4!")
    end
    return cs.continue
end)

plugin:command("css_luahello", function(player, command)
    command:reply("Lua command executed.")
end)
```

Each top-level `.lua` file in the scripts directory is an independent plugin and Lua VM. Files beginning with `_` and files in subdirectories are treated as modules rather than plugins.

## Management commands

```text
css_lua list
css_lua load <script>
css_lua reload <script>
css_lua unload <script>
css_lua reload_all
```

The default required permission is `@css/root`. Server console and RCON may always use the management command.

When automatic reload is enabled, editing a top-level script reloads that plugin. Editing a module in a subdirectory reloads all Lua plugins. Syntax and registration validation happen in a new VM before the active version is replaced. A failed reload keeps or restores the previous version.

## Configuration

CounterStrikeSharp creates `addons/counterstrikesharp/configs/plugins/Lua2CS/Lua2CS.json`:

```json
{
  "ScriptsDirectory": "scripts",
  "AutoReload": true,
  "ReloadDebounceMilliseconds": 400,
  "AdminPermission": "@css/root",
  "AllowUnsafeLibraries": false,
  "ConfigVersion": 1
}
```

By default Lua scripts cannot use `luanet`, native modules, process execution, or direct file I/O. `AllowUnsafeLibraries` restores the Lua file and OS libraries but never exposes `luanet`.

See [docs/lua-api.md](docs/lua-api.md) for the complete scripting API and [examples](examples) for working scripts.

## Building

```bash
dotnet restore
dotnet test -c Release
./package.sh
```

The packaged server archive is written to `artifacts/Lua2CS-preview-linux-x64.zip`.

## License

MIT
