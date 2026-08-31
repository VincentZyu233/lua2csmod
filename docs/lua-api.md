# Lua 脚本接口

## 插件信息与生命周期

每个顶层脚本必须且只能调用一次 `cs.plugin`：

```lua
local plugin = cs.plugin({
    name = "必填插件名",
    version = "1.0.0",
    description = "可选说明"
})

plugin:on_load(function(hot_reload) end)
plugin:on_unload(function(hot_reload) end)
```

`hot_reload` 表示本次加载或卸载是否由热重载引起。从生命周期回调返回 `false` 会拒绝激活；脚本准备或激活期间抛出异常也会拒绝本次加载。

## 游戏事件

```lua
local id = plugin:on("player_death", function(event, info)
    if event.attacker ~= nil then
        event.attacker:print_chat("已记录击杀")
    end
    return cs.continue
end, { mode = "post" })
```

事件名使用 CounterStrikeSharp 的标准名称，例如 `player_chat`、`round_start` 和 `player_death`。`mode` 可设为 `pre` 或 `post`，默认为 `post`。

C# 事件属性会转换成蛇形命名的 Lua 字段，例如 `DmgArmor` 转换为 `dmg_armor`。玩家类型字段会转换为玩家表。当事件具有可解析的 `userid` 字段时，还会提供便捷字段 `event.player`。

Pre 回调可以修改事件字段和 `info.dont_broadcast`。事件回调应返回以下值之一：

- `cs.continue`：继续执行其他 Hook。
- `cs.changed`：标记事件已经修改。
- `cs.handled`：阻止原始行为，但允许后续 Hook。
- `cs.stop`：阻止原始行为和后续 Hook。

## Listener

```lua
plugin:listen("OnMapStart", function(map_name)
    cs.log.info("地图已开始：" .. map_name)
end)
```

Listener 名称与 CounterStrikeSharp 的 `Listeners` 委托一致。基础类型和玩家参数会转换为 Lua 值；当前无法安全包装的原生对象会转换为字符串。需要返回 HookResult 的 Listener 使用与游戏事件相同的返回值。

应避免在 `OnTick` 中执行高开销操作，因为 CS2 服务器每秒会触发 64 次该 Listener。

## 命令

```lua
plugin:command("css_greet", {
    description = "问候一名玩家",
    permission = "@css/generic",
    allow_console = true,
    min_args = 1,
    usage = "<名字>"
}, function(player, command)
    command:reply("你好，" .. command.args[1])
end)
```

从服务器控制台或 RCON 执行命令时，`player` 为 `nil`。命令对象包含以下成员：

- `name`：实际命令名。
- `args`：从 1 开始的参数表，不包含命令名。
- `arg_string`：原始参数字符串。
- `context`：命令调用上下文。
- `command:reply(message)`：向调用者回复。

不需要选项时可以省略选项表：

```lua
plugin:command("css_ping", function(player, command)
    command:reply("pong")
end)
```

## 定时器

```lua
local timer_id = plugin:timer(5, function()
    cs.log.info("已经过去五秒")
end, {
    repeating = true,
    stop_on_map_change = true
})

plugin:cancel(timer_id)
```

- `repeating`：是否循环执行，默认为 `false`。
- `stop_on_map_change`：换图时是否停止，默认为 `true`。

事件、Listener、命令和定时器注册都会返回 ID，可传给 `plugin:cancel`。脚本卸载或重载时，剩余注册项会自动清理。

## 服务器接口

```lua
cs.server.print_chat_all("发送给所有玩家")
cs.server.print_console("输出到服务器控制台")
cs.server.execute("sv_cheats 0")

cs.log.debug("调试日志")
cs.log.info("普通日志")
cs.log.warn("警告日志")
cs.log.error("错误日志")
```

聊天控制码位于 `cs.colors`，包括 `default`、`green`、`lime`、`red`、`yellow`、`blue`、`purple`、`grey`、`gold` 和 `orange` 等。

```lua
cs.server.print_chat_all(cs.colors.green .. "绿色消息" .. cs.colors.default)
```

## 玩家接口

```lua
for _, player in ipairs(cs.players.all()) do
    player:print_chat("你好，" .. player.name)
end

local player = cs.players.get(0)
```

玩家表字段：

- `slot`：玩家槽位。
- `name`：玩家名。
- `steam_id`：SteamID64 字符串。
- `team`：当前队伍。
- `is_bot`：是否为机器人。
- `is_hltv`：是否为 HLTV。
- `is_alive`：当前是否存活。

玩家方法：

- `player:print_chat(message)`
- `player:print_console(message)`
- `player:print_center(message)`

玩家表是短期视图。玩家断开连接或换图后，应通过 `cs.players.get(slot)` 重新获取，不要长期保存旧玩家表。

## 模块

脚本可以通过 `require("module")` 加载同目录模块，也可以通过 `require("folder.module")` 加载子目录模块。

```text
scripts/
├── gameplay.lua
├── _shared.lua
└── lib/
    └── messages.lua
```

`gameplay.lua` 会作为独立插件加载；`_shared.lua` 和 `lib/messages.lua` 只作为模块使用。修改子目录模块会触发全部 Lua 插件重载。

## 热重载行为

热重载按以下顺序执行：

1. 在新 Lua VM 中读取并执行新脚本。
2. 验证插件信息、事件、Listener、命令冲突和定时器参数。
3. 新版本验证失败时保持旧版本运行。
4. 暂停旧版本注册项并激活新版本。
5. 新版本激活失败时清理新资源并恢复旧版本。
6. 成功后调用旧版本 `on_unload(true)` 并销毁旧 VM。

文件监听回调不会直接访问游戏 API，真正的重载会通过 `Server.NextWorldUpdate` 回到游戏线程执行，即使服务器处于休眠状态也可以处理。
