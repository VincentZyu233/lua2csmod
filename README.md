# Lua2CS

Lua2CS 是面向 CounterStrikeSharp 的 Lua 5.4 插件宿主。C# 宿主只需安装一次，之后即可直接编写、加载和热重载 Lua 玩法脚本，无需反复编译 DLL 或重启 CS2 服务器。

## 环境要求

- Linux x64 CS2 专用服务器
- Metamod:Source
- CounterStrikeSharp API 1.0.373 或更高版本，并安装 .NET 10 运行时

## 安装

1. 从预发布版本下载 `Lua2CS-preview-linux-x64.zip`。
2. 将压缩包解压到服务器的 `game/csgo` 目录。
3. 把 Lua 脚本放入 `addons/counterstrikesharp/plugins/Lua2CS/scripts`。
4. 重启服务器，或执行 `css_plugins load Lua2CS`。

压缩包已经包含 NLua、KeraLua 和 Linux x64 Lua 5.4 原生库，不需要在服务器上额外安装 Lua。

## 脚本示例

```lua
local plugin = cs.plugin({
    name = "你好 Lua",
    version = "1.0.0"
})

plugin:on("player_chat", function(event)
    if event.player ~= nil and event.text:lower() == "hello" then
        event.player:print_chat("你好，消息来自 Lua 5.4！")
    end
    return cs.continue
end)

plugin:command("css_luahello", function(player, command)
    command:reply("Lua 命令执行成功。")
end)
```

`scripts` 目录中的每个顶层 `.lua` 文件都是一个独立插件，并拥有独立的 Lua VM。以下划线开头的文件和子目录中的文件只会作为模块使用，不会被当作独立插件加载。

## 管理命令

```text
css_lua list
css_lua load <脚本名>
css_lua reload <脚本名>
css_lua unload <脚本名>
css_lua reload_all
```

默认要求 `@css/root` 权限。服务器控制台和 RCON 始终可以使用管理命令。在游戏聊天中也可通过 CounterStrikeSharp 的命令触发方式执行，例如 `!lua list`。

开启自动重载后，修改顶层脚本只会重载对应插件；修改子目录中的公共模块会重载全部 Lua 插件。新脚本会先在独立 VM 中完成语法检查、执行和注册验证，再替换当前版本。重载失败时会保留或恢复旧版本。

## 配置

CounterStrikeSharp 会生成 `addons/counterstrikesharp/configs/plugins/Lua2CS/Lua2CS.json`：

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

- `ScriptsDirectory`：相对于 Lua2CS 插件目录的脚本目录，不允许逃逸到插件目录之外。
- `AutoReload`：监听 `.lua` 文件变化并自动重载。
- `ReloadDebounceMilliseconds`：文件变化防抖时间，允许范围为 100 到 5000 毫秒。
- `AdminPermission`：游戏内管理命令要求的 CounterStrikeSharp 权限；留空表示不检查权限。
- `AllowUnsafeLibraries`：恢复 Lua 文件和操作系统库。即使开启，也不会暴露 `luanet`。

默认情况下，Lua 脚本不能访问 `luanet`、原生模块、进程执行或直接文件 I/O。`require` 仍可加载当前脚本目录中的 Lua 模块。

完整接口参见 [Lua 脚本接口](docs/lua-api.md)，可运行示例位于 [examples](examples)。

## 本地构建

```bash
dotnet restore
dotnet test -c Release
./package.sh
```

服务器安装包会生成到 `artifacts/Lua2CS-preview-linux-x64.zip`。

## 许可证

MIT
