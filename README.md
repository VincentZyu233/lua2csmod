# Lua2CS

Lua2CS 是面向 CounterStrikeSharp 的 Lua 5.4 插件宿主。C# 宿主只需安装一次，之后即可直接编写、加载和热重载 Lua 玩法脚本，无需反复编译 DLL 或重启 CS2 服务器。

## 环境要求

- Linux x64 CS2 专用服务器
- Metamod:Source
- CounterStrikeSharp API 1.0.373 或更高版本，并安装 .NET 10 运行时

## 安装

1. 从 [预发布版本](https://github.com/ra1nyxin/lua2csmod/releases/tag/preview) 下载 `Lua2CS-preview-linux-x64.zip`。
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

## 接口范围

Lua 脚本可以使用以下能力：

- 游戏事件、Listener、命令、一次性和循环定时器。
- 玩家列表、CSS 原生目标语法，以及按槽位、userid、SteamID64 和名字查询玩家。
- 玩家生命、护甲、金钱、坐标、视角、武器、按键、队伍和回合状态快照。
- 玩家聊天、控制台、中央提示、HTML HUD、权限、客户端命令、声音、传送、复活、队伍和武器操作。
- 服务器地图、时间、Tick、地图列表、模型预缓存和控制台命令。
- ConVar 读取与修改、官方事件名和 Listener 名枚举。
- 游戏规则、回合阶段、炸弹状态和双方比分查询。
- 插件级 JSON 持久化，以及带完整 CHandle 校验的实体查询、创建、输入、传送和删除。
- 聊天/控制台菜单、回合结束控制、准星目标和导航网格查询。
- 玩家与实体的最大生命、重力、速度倍率、模型和渲染颜色控制。
- 玩家详细武器与弹药快照、弹药修改、计分板数据和语音标志控制。
- 单客户端 ConVar 复制和机器人客户端 ConVar 修改。

Lua 与 C# 之间的字符串统一使用 UTF-8，可直接使用中文插件名、命令说明、聊天文本和持久化内容。修改生命、武器、队伍、实体、传送和 ConVar 等操作会直接影响服务器状态，应只向可信脚本开放。

## 示例模板

`examples` 中包含 35 个可独立加载的中文模板和 1 个公共模块示例，覆盖基础命令、聊天与冷却、事件统计、回合玩法、管理员工具、菜单、玩家和武器、弹药、计分板、语音、ConVar、实体、导航、模型、持久化、HUD、地图及模块化脚本。模板包含关键流程、快照时效、身份校验和风险点的中文注释。

模板默认不会自动运行；安装包将它们放在插件的 `examples` 目录中。选中模板后复制到同级 `scripts` 目录即可加载，后续保存文件会自动热重载。

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
