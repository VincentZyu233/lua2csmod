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

还可使用两个语义更明确的快捷方法：

```lua
plugin:after(2, function()
    cs.log.info("两秒后执行一次")
end)

plugin:every(10, function()
    cs.log.info("每十秒执行一次")
end, { stop_on_map_change = true })
```

一次性定时器执行完毕后会自动从插件注册表移除。

## 服务器接口

```lua
cs.server.print_chat_all("发送给所有玩家")
cs.server.print_console("输出到服务器控制台")
cs.server.execute("sv_cheats 0")

local info = cs.server.info()
local maps = cs.server.maps()
local valid = cs.server.is_map_valid("de_dust2")
cs.server.precache_model("models/example/example.vmdl")

cs.log.debug("调试日志")
cs.log.info("普通日志")
cs.log.warn("警告日志")
cs.log.error("错误日志")
```

`cs.server.info()` 返回：

- `map_name`：当前地图名。
- `max_players`：服务器最大玩家数。
- `tick_interval`：单 Tick 秒数，CS2 通常为 `0.015625`。
- `tick_count`：当前地图 Tick 数。
- `current_time`：当前地图时间。
- `ticked_time`：服务器已模拟时间，休眠时不增长。
- `engine_time`：引擎运行时间，休眠时仍增长。
- `frame_time`：上一帧耗时。

`cs.server.maps()` 从服务器的 `maplist.txt` 返回有效地图数组。`cs.server.execute` 直接执行服务器命令，不应拼接未经验证的玩家输入；切图前应先调用 `is_map_valid`。

## ConVar

```lua
local gravity = cs.convars.get("sv_gravity")
if gravity ~= nil then
    cs.log.info("当前重力：" .. gravity)
end

if not cs.convars.set("sv_gravity", 600) then
    cs.log.warn("找不到 sv_gravity")
end
```

- `cs.convars.get(name)`：返回 ConVar 的字符串值；不存在时返回 `nil`。
- `cs.convars.set(name, value)`：修改 ConVar，成功返回 `true`，不存在返回 `false`。

ConVar 修改是服务器全局状态，不会随 Lua 插件卸载自动恢复。

聊天控制码位于 `cs.colors`，包括 `default`、`green`、`lime`、`red`、`yellow`、`blue`、`purple`、`grey`、`gold` 和 `orange` 等。

```lua
cs.server.print_chat_all(cs.colors.green .. "绿色消息" .. cs.colors.default)
```

## 玩家接口

```lua
for _, player in ipairs(cs.players.all()) do
    player:print_chat("你好，" .. player.name)
end

local by_slot = cs.players.get(0)
local by_userid = cs.players.get_userid(12)
local by_steamid = cs.players.get_steamid("76561198000000000")
local matches = cs.players.find("名字片段")
```

玩家集合接口：

- `cs.players.all()`：所有有效玩家，包括机器人和 HLTV。
- `cs.players.humans()`：排除机器人和 HLTV 的玩家。
- `cs.players.bots()`：机器人玩家。
- `cs.players.count()`：有效玩家数量。
- `cs.players.get(slot)`：按槽位查找。
- `cs.players.get_userid(userid)`：按当前连接的 userid 查找。
- `cs.players.get_steamid(steam_id)`：按 SteamID64 字符串查找。
- `cs.players.find(query)`：按槽位、userid、SteamID64 或不区分大小写的名字片段查询，始终返回数组。

`find` 可能匹配多名玩家，执行管理操作前必须检查 `#matches == 1`。

玩家快照字段：

- `slot`：玩家槽位。
- `user_id`：本次连接的 userid。
- `name`：玩家名。
- `steam_id`：SteamID64 字符串。
- `ip_address`：客户端 IP 地址，可能包含端口；机器人通常为回环地址。
- `team`：当前队伍名称。
- `team_id`：队伍数字，见 `cs.team`。
- `is_bot`：是否为机器人。
- `is_hltv`：是否为 HLTV。
- `is_alive`：当前是否存活。
- `ping`：延迟。
- `score`、`round_score`、`mvps`：计分板数据。
- `health`、`armor`、`money`：生命、护甲和金钱；无有效 Pawn 时可能为 `nil`。
- `has_helmet`、`has_defuser`：是否有头盔或拆弹器。
- `in_buy_zone`、`in_bomb_zone`：是否位于购买区或炸弹区。
- `buttons`：当前按键位掩码，可结合 `cs.buttons` 判断。
- `position`、`velocity`、`eye_angles`：包含 `x`、`y`、`z` 的向量表。
- `active_weapon`：当前武器的 Designer Name，可能为 `nil`。
- `weapons`：当前持有武器的 Designer Name 数组。

玩家方法：

- `player:print_chat(message)`
- `player:print_console(message)`
- `player:print_center(message)`
- `player:print_alert(message)`
- `player:print_html(html, duration)`：在中央显示 HTML，时长限制为 1 到 60 秒。
- `player:refresh()`：按原槽位重新获取最新玩家快照，玩家已离开时返回 `nil`。
- `player:has_permission(permission)`：检查 CounterStrikeSharp 管理权限。
- `player:can_target(target)`：按 CSS 免疫等级检查是否可管理目标。
- `player:get_convar(name)`：读取该客户端报告的 ConVar 值。
- `player:execute(command)`：让客户端执行允许客户端执行的命令。
- `player:execute_as_server(command)`：以该玩家上下文执行服务器侧客户端命令。
- `player:give_item(designer_name)`、`player:remove_item(designer_name)`
- `player:remove_weapons()`、`player:drop_active_weapon()`
- `player:respawn()`、`player:kill(explode, force)`、`player:kick()`
- `player:change_team(team)`：遵循游戏规则换队，通常会死亡并丢失装备。
- `player:switch_team(team)`：强制换队并保留存活状态和装备。
- `player:teleport(position, angles, velocity)`：三个参数均可为 `nil`，但至少提供一个向量。
- `player:set_health(value)`、`player:set_armor(value)`、`player:set_money(value)`

修改方法在成功找到有效玩家或 Pawn 时返回 `true`，否则返回 `false`。无返回结果的 CSS 底层操作只能表示已成功提交，不能保证游戏规则不会随后覆盖该状态。

玩家表是短期视图。玩家断开连接或换图后，应通过 `cs.players.get(slot)` 重新获取，不要长期保存旧玩家表。

`ip_address` 属于敏感信息，不应写入公开日志或发送给无管理权限的玩家。

## 向量、队伍与按键

```lua
local zero = cs.vec3(0, 0, 0)
player:teleport(cs.vec3(100, 200, 300), cs.angle(0, 90, 0), zero)
player:switch_team(cs.team.ct)

local is_jumping = (player.buttons & cs.buttons.jump) ~= 0
```

向量既可使用 `x/y/z`，也可使用数组索引 `1/2/3`。事件中的 Vector 和 QAngle 字段同样使用此格式。

`cs.team` 包含 `none`、`spectator`、`terrorist`/`t`、`counter_terrorist`/`ct`。换队方法也接受字符串 `none`、`spec`、`t`、`ct` 或数字 0 到 3。

`cs.buttons` 包含 `attack`、`jump`、`duck`、`forward`、`back`、`use`、`left`、`right`、`move_left`、`move_right`、`attack2`、`reload`、`speed`、`walk`、`zoom`、`scoreboard` 和 `inspect`。

## 能力发现

```lua
for _, event_name in ipairs(cs.capabilities.events()) do
    cs.log.debug(event_name)
end

for _, listener_name in ipairs(cs.capabilities.listeners()) do
    cs.log.debug(listener_name)
end
```

这两个接口返回当前安装的 CounterStrikeSharp 版本实际提供的官方事件名和 Listener 名，可用于排查版本差异。

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

## 示例模板索引

| 文件 | 用途 | 主要接口 |
| --- | --- | --- |
| `hello.lua` | 最小命令插件 | 命令、生命周期 |
| `qwq.lua` | 聊天关键词回复 | 游戏事件、聊天颜色 |
| `round_timer.lua` | 回合提示和循环消息 | 游戏事件、定时器 |
| `admin_tools.lua` | 治疗、发枪、换队 | 玩家查询、权限、武器、状态 |
| `spawn_protection.lua` | 出生后临时增加生命 | 事件、一次性定时器、玩家状态 |
| `round_loadout.lua` | 每回合统一装备 | 玩家集合、武器、护甲 |
| `kill_reward.lua` | 击杀增加金钱 | 击杀事件、金钱、中央提示 |
| `player_hud.lua` | 状态 HTML HUD | 循环定时器、玩家快照 |
| `join_messages.lua` | 玩家进出服播报 | Listener、延迟回调 |
| `map_tools.lua` | 服务器信息与安全切图 | 服务器信息、地图校验 |
| `checkpoints.lua` | 保存并返回传送点 | 向量、坐标、传送 |
| `player_info.lua` | 查询玩家和武器信息 | 玩家查找、完整快照 |
| `module_demo.lua` | 引用子目录公共模块 | `require`、模块拆分 |

安装包中的模板位于 `addons/counterstrikesharp/plugins/Lua2CS/examples`。复制需要启用的顶层模板到同级 `scripts` 目录；`module_demo.lua` 还需要同时复制 `modules` 子目录。
