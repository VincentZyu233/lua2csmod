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

## 菜单

```lua
cs.menu.open(player, {
    title = "选择功能",
    type = "chat",
    exit_button = true,
    post_select = "close",
    items = {
        { text = "补满生命" },
        { text = "暂不可用", disabled = true }
    }
}, function(selected_player, index)
    if index == 1 then selected_player:set_health(100) end
end)
```

- `type`：`chat` 在聊天区显示，`console` 在玩家控制台显示。
- `items`：1 到 64 个选项；`text` 必填，`disabled` 默认为 `false`。
- `exit_button`：是否显示退出项，默认为 `true`。
- `post_select`：选择后执行 `close`、`reset` 或 `nothing`，默认为 `reset`。
- 回调参数是选择选项时的最新玩家快照和从 1 开始的选项索引。
- `cs.menu.close(player)` 或 `player:close_menu()`：关闭玩家当前的 CSS 菜单；没有活动菜单时返回 `false`。

菜单标题最长 256 字符，选项最长 512 字符，均不允许换行或空字符。插件卸载或热重载时会关闭仍属于该插件的活动菜单，避免旧回调访问已经销毁的 Lua VM。当前只包装聊天和控制台菜单；中央 HTML 菜单需要额外的 Tick Listener 生命周期，暂不开放。

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

## 游戏规则

```lua
local rules = cs.game.rules()
if rules ~= nil then
    cs.log.info(string.format("T %d : %d CT", rules.terrorist_score or 0, rules.ct_score or 0))
end
```

`cs.game.rules()` 在游戏规则实体可用时返回快照，否则返回 `nil`。字段包括：

- `freeze_period`、`warmup_period`：是否处于冻结或热身阶段。
- `warmup_start_time`、`warmup_end_time`、`round_start_time`：对应阶段的服务器时间。
- `game_restart`、`game_phase`、`total_rounds_played`、`overtime_playing`：比赛进程状态。
- `bomb_planted`、`bomb_dropped`：C4 状态。
- `ct_timeout_active`、`terrorist_timeout_active`：双方暂停状态。
- `terrorist_score`、`ct_score`：双方当前比分；队伍管理实体尚未就绪时字段可能为 `nil`。

返回值是调用时的只读快照。需要最新状态时应重新调用，而不是长期保存旧表。

管理员脚本可以主动结束回合：

```lua
cs.game.terminate_round(1, cs.round_end.ct_win)
```

延迟必须是 0 到 60 秒。常用原因位于 `cs.round_end`：`target_bombed`、`bomb_defused`、`ct_win`、`terrorist_win`、`draw`、`all_hostages_rescued`、`target_saved` 和 `game_commencing`。接口成功提交时返回 `true`；游戏规则实体尚未就绪时返回 `false`。结束回合会立即改变比赛流程，只应由受信任的管理命令调用。

## 持久化存储

```lua
local visits = cs.storage.get("visits", 0) + 1
cs.storage.set("visits", visits)
cs.storage.set("enabled", true)
cs.storage.set("message", "中文内容")

local exists = cs.storage.has("visits")
local snapshot = cs.storage.all()
cs.storage.delete("message")
cs.storage.set("enabled", nil) -- 等同于删除
cs.storage.clear()
```

每个顶层插件使用独立文件 `scripts/.lua2cs-data/<脚本名>.json`。接口不接受文件路径，也不会允许脚本越过该目录访问文件系统。Lua 与 JSON 均使用 UTF-8。

- `cs.storage.get(key, default)`：键不存在时返回 `default`；省略默认值时返回 `nil`。
- `cs.storage.has(key)`：判断键是否存在。
- `cs.storage.set(key, value)`：保存字符串、布尔值、整数或有限小数；成功返回 `true`。
- `cs.storage.set(key, nil)`、`cs.storage.delete(key)`：删除键；只有键原本存在时返回 `true`。
- `cs.storage.all()`：返回当前全部键值的快照。
- `cs.storage.clear()`：清空当前插件的数据。

键去除首尾空白后必须为 1 到 128 个非控制字符。不支持表、函数、userdata、`NaN` 和正负无穷。每次操作都会重新读取文件并以临时文件原子替换，避免热重载中新旧 VM 的缓存互相覆盖。JSON 损坏时会记录警告并按空数据处理。

## 玩家接口

```lua
for _, player in ipairs(cs.players.all()) do
    player:print_chat("你好，" .. player.name)
end

local by_slot = cs.players.get(0)
local by_userid = cs.players.get_userid(12)
local by_steamid = cs.players.get_steamid("76561198000000000")
local matches = cs.players.find("名字片段")
local targets = cs.players.target("@alive", caller)
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
- `cs.players.target(pattern, caller)`：使用 CounterStrikeSharp 原生目标选择器，始终返回数组；`caller` 可为玩家表或 `nil`。

`find` 可能匹配多名玩家，执行管理操作前必须检查 `#matches == 1`。

原生目标选择器支持 `@all`、`@bots`、`@human`、`@alive`、`@dead`、`@me`、`@!me`、`@ct`、`@t`、`@spec`、`@aim`、`#userid`、SteamID 和名字。`@me`、`@!me`、`@aim` 依赖有效调用者；服务器控制台调用命令时 `caller` 为 `nil`。目标选择只负责匹配，管理操作仍应使用 `caller:can_target(target)` 检查 CSS 免疫等级。

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
- `score`、`round_score`、`rounds_won`、`mvps`：计分板数据。
- `teammate_color`：竞技队友颜色索引。
- `language`：玩家通过 CSS 选择的语言名称，例如 `zh-CN`；未设置时使用服务器默认语言。
- `voice_flags`：当前语音位标志，可结合 `cs.voice` 判断。
- `health`、`armor`、`money`：生命、护甲和金钱；无有效 Pawn 时可能为 `nil`。
- `max_health`、`gravity_scale`、`velocity_modifier`：最大生命、重力比例和受击移动速度倍率。
- `has_helmet`、`has_defuser`：是否有头盔或拆弹器。
- `in_buy_zone`、`in_bomb_zone`：是否位于购买区或炸弹区。
- `is_scoped`、`is_defusing`、`is_grabbing_hostage`、`is_walking`：瞄准镜、拆弹、人质和行走状态。
- `shots_fired`、`flags`：连续射击计数和实体标志位。
- `buttons`：当前按键位掩码，可结合 `cs.buttons` 判断。
- `position`、`velocity`、`eye_angles`：包含 `x`、`y`、`z` 的向量表。
- `active_weapon`：当前武器的 Designer Name，可能为 `nil`。
- `weapons`：当前持有武器的 Designer Name 数组。
- `active_weapon_info`：当前武器详细快照，可能为 `nil`。
- `weapon_details`：与 `weapons` 顺序一致的详细武器快照数组。
- `model`、`render_color`：当前模型路径和包含 `red/green/blue/alpha` 的渲染颜色。

武器详细快照字段包括 `handle`、`index`、`designer_name`、`clip`、`reserve`、`reserve_secondary` 和 `item_definition_index`。武器切换、丢弃或删除后旧快照不会自动更新。

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
- `player:aim_target()`：返回准星所指玩家的最新快照；没有玩家目标或游戏规则未就绪时返回 `nil`。
- `player:set_max_health(value)`：设置最大生命，限制为至少 1。
- `player:set_gravity(scale)`：设置重力比例，限制为 0 到 10。
- `player:set_velocity_modifier(value)`：设置受击移动速度倍率，限制为 0 到 10；游戏逻辑可能随后重置。
- `player:set_model(model_name, precache)`：设置玩家模型；`precache` 默认为 `true`。
- `player:set_render_color(red, green, blue, alpha)`：设置 Pawn 渲染颜色，各通道限制为 0 到 255，透明度默认为 255。
- `player:close_menu()`：关闭当前 CSS 菜单。
- `player:set_ammo(clip, reserve)`：修改当前武器弹匣和主备弹，允许其中一个为 `nil`，但不能同时省略；数量限制为 0 到 10000。
- `player:set_score(value)`、`player:set_round_score(value)`、`player:set_mvps(value)`：修改非负计分板计数；游戏规则可能随后重新计算。
- `player:set_voice_flags(flags)`：设置 0 到 31 的 CSS 语音位标志，通常使用 `cs.voice` 常量或按位组合。
- `player:replicate_convar(name, value)`：只向该客户端复制一个 ConVar 值，不修改服务器全局值。
- `player:set_fake_convar(name, value)`：设置机器人报告的客户端 ConVar；目标不是真实机器人时返回 `false`。
- `player:emit_sound(sound_event_name, volume, pitch)`：只向该玩家播放声音事件；音量默认为 1，音调默认为 0，返回声音实体 GUID，失败返回 0。

修改方法在成功找到有效玩家或 Pawn 时返回 `true`，否则返回 `false`。无返回结果的 CSS 底层操作只能表示已成功提交，不能保证游戏规则不会随后覆盖该状态。

ConVar 名称去除首尾空白后必须为 1 到 128 个不含空白或控制字符的字符；复制值最长 4096 字符，不能含换行或空字符。`replicate_convar` 是否对客户端产生可见效果取决于目标 ConVar 的引擎行为。

玩家表是短期视图。所有玩家方法都会同时校验槽位、userid 和 SteamID64，旧表不会因槽位被新玩家复用而误操作新人。玩家断开连接或换图后，应通过查询接口重新获取，不要长期保存旧玩家表。

`ip_address` 属于敏感信息，不应写入公开日志或发送给无管理权限的玩家。

## 实体接口

```lua
local doors = cs.entities.find("func_door", 32)
local entity = doors[1] and cs.entities.get(doors[1].index) or nil
local prop = cs.entities.create("prop_dynamic", false)
if prop ~= nil then prop:spawn() end

if entity ~= nil then
    entity:input("Open")
    entity:teleport(cs.vec3(0, 0, 128), nil, nil)
    entity:emit_sound("Example.Sound", 1, 0)
end
```

- `cs.entities.find(designer_name, limit)`：按精确 Designer Name 查询，`limit` 默认为 128，限制为 1 到 512，返回数组。
- `cs.entities.get(index)`：按当前实体索引获取快照；索引无效时返回 `nil`。
- `cs.entities.create(designer_name, spawn)`：创建实体，`spawn` 默认为 `true`；失败返回 `nil`。Designer Name 只允许 ASCII 字母、数字和下划线。

实体快照字段包括 `handle`、`index`、`designer_name`、`name`、`health`、`max_health`、`team_id`、`gravity_scale`、`flags`、`spawn_flags`、`position`、`rotation` 和 `velocity`。部分实体没有名字或坐标，对应字段可能为 `nil`。

实体方法：

- `entity:refresh()`：通过完整句柄重新读取快照；实体已失效时返回 `nil`。
- `entity:spawn()`：对尚未生成的实体执行 DispatchSpawn。
- `entity:input(input_name, value, delay)`：立即或延迟发送 Source 2 I/O 输入，`value` 默认为空字符串。
- `entity:remove(delay)`：立即或延迟删除实体。
- `entity:teleport(position, angles, velocity)`：三个向量均可为 `nil`，但至少提供一个。
- `entity:set_health(value)`：设置生命值并通知网络状态变化。
- `entity:set_max_health(value)`：设置最大生命值。
- `entity:set_gravity(scale)`：设置 0 到 10 的重力比例。
- `entity:set_model(model_name, precache)`：为模型实体设置模型；`precache` 默认为 `true`。
- `entity:set_render_color(red, green, blue, alpha)`：为模型实体设置渲染颜色。
- `entity:emit_sound(sound_event_name, volume, pitch)`：从实体播放声音，返回声音实体 GUID，失败返回 0。

实体表保存的是包含索引和序列号的完整 32 位 CHandle，而不是裸索引；实体被删除并复用索引后，旧表的方法会返回失败，不会操作新实体。延迟参数限制为 0 到 3600 秒；声音音量会限制在 0 到 1，音调会限制在 0 到 255，且不接受 `NaN` 或无穷。

实体创建、重复生成、删除、模型修改和任意 I/O 输入可能破坏地图逻辑、导致崩服或制造无法清理的实体，只应允许受信任脚本使用。`create(..., false)` 创建的实体不会自动 DispatchSpawn，调用者完成生成前配置后需要调用 `entity:spawn()`；不得对已经生成的实体重复调用。CounterStrikeSharp 目前不能可靠查询任意实体的 C++ 继承关系，因此只有明确知道目标属于 `CBaseModelEntity` 时才能调用实体的 `set_model` 和 `set_render_color`；不确定时不要调用。

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

`cs.voice` 包含 `normal`、`muted`、`all`、`listen_all`、`team` 和 `listen_team`。除 `normal` 外均为位标志，可用 Lua 5.4 的 `|` 组合，例如 `cs.voice.team | cs.voice.listen_team`。

## 导航网格

```lua
local area = cs.nav.closest(player.position, 512)
if area ~= nil then
    cs.log.info("最近导航区域：" .. area.id .. "，距离：" .. area.distance)
end

local first_areas = cs.nav.areas(128)
```

- `cs.nav.closest(position, maximum_distance)`：返回最近导航区域，默认不限制距离；没有可用导航网格或指定范围内无结果时返回 `nil`。
- `cs.nav.areas(limit)`：返回当前地图的导航区域数组，默认最多 1024 个，限制为 1 到 4096。

导航区域字段包括 `id`、`center`、`normal`、`min`、`max`、`width`、`height` 和 `area_2d`；`closest` 还会提供 `distance`。地图切换后旧快照只剩普通数值，不应视为新地图的有效区域。枚举全部导航区域有一定开销，不要在 `OnTick` 中反复调用。

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

验证过程会执行新脚本的顶层代码，因此它不是事务沙箱。不要在脚本顶层创建实体、执行服务器命令、修改 ConVar 或写入持久化数据；把有副作用的初始化放入 `on_load`。即使如此，`on_load` 已经完成的外部状态修改也无法在后续语句失败时自动回滚，复杂初始化应自行使用 `on_unload` 或错误处理清理资源。

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
| `persistent_kills.lua` | 跨热重载保存累计击杀 | 安全持久化、击杀事件 |
| `target_tools.lua` | 按 CSS 目标语法批量设置护甲 | 原生目标选择、免疫检查 |
| `game_status.lua` | 查询回合阶段与比分 | 游戏规则快照 |
| `entity_tools.lua` | 查询实体并发送 I/O 输入 | 实体快照、完整句柄、实体输入 |
| `menu_demo.lua` | 打开聊天菜单并处理选择 | 菜单、回调、玩家操作 |
| `movement_fun.lua` | 批量调整重力和速度倍率 | 原生目标选择、玩家移动参数 |
| `nav_tools.lua` | 查询玩家附近导航区域 | 坐标、导航网格 |
| `round_control.lua` | 管理员主动结束回合 | 游戏规则、回合结束原因 |
| `model_tools.lua` | 批量设置模型和渲染颜色 | 玩家模型、预缓存、颜色 |
| `ammo_refill.lua` | 查看并补充当前武器弹药 | 武器详细快照、弹匣、备弹 |
| `weapon_inspector.lua` | 列出玩家武器详细信息 | 玩家查询、武器快照 |
| `scoreboard_tools.lua` | 批量修改计分板数据 | 目标选择、分数、MVP |
| `voice_tools.lua` | 设置玩家语音模式 | 语音位标志、权限检查 |
| `client_convar.lua` | 向客户端复制 ConVar | 目标选择、客户端 ConVar |
| `bot_convar.lua` | 设置机器人客户端 ConVar | 机器人集合、FakeClient ConVar |
| `damage_report.lua` | 统计并播报回合伤害 | 伤害事件、回合事件、短期状态 |
| `chat_cooldown.lua` | 带玩家冷却的聊天关键词 | 聊天 Listener、服务器时间 |
| `random_loadout.lua` | 玩家出生时随机发放装备 | 出生事件、随机数、身份刷新 |
| `welcome_menu.lua` | 玩家入服后显示欢迎菜单 | Listener、延迟回调、菜单 |
| `bomb_announcer.lua` | 中文播报 C4 状态变化 | 炸弹事件、聊天颜色 |
| `aim_inspector.lua` | 查询准星所指玩家 | 准星目标、玩家快照 |
| `team_summary.lua` | 汇总双方人数和存活状态 | 玩家集合、队伍常量 |

安装包中的模板位于 `addons/counterstrikesharp/plugins/Lua2CS/examples`。复制需要启用的顶层模板到同级 `scripts` 目录；`module_demo.lua` 还需要同时复制 `modules` 子目录。
