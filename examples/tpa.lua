-- TPA 玩家传送请求示例（所有指令只能由玩家执行）：
-- !tpa <玩家>：请求传送到指定玩家身边；不能选择自己、Bot 或 HLTV。
-- !tpaccept [玩家]：接受请求；省略玩家时处理最新收到的有效请求。
-- !tpdeny [玩家]：拒绝请求；省略玩家时处理最新收到的有效请求。
-- !tpcancel：取消自己当前发出的请求。
-- <玩家> 支持名字、slot、userid 或 SteamID64；多人匹配时必须写得更完整。
-- 请求 30 秒后过期；每名请求者同时只能保留一个请求，接收者可收到多个请求。
-- 接受后，请求者会传送到接收者的位置和视角，并清除原有移动速度。
-- 双方必须仍在线且存活，接收者还必须有有效坐标。
-- 本示例不限制阵营、回合、冻结时间、战斗状态、空中状态或地图边界。
-- 任一方离服时，相关请求都会自动取消并通知仍在线的一方。

local plugin = cs.plugin({
    name = "玩家传送请求",
    version = "1.2.0",
    description = "提供不限制阵营和回合状态的宽松玩家传送请求"
})

local request_timeout = 30
local next_request_id = 0

-- 请求全部以 SteamID64 关联，不能用 slot 保存长期身份：玩家离服后 slot 会被复用。
-- outgoing 的键是请求者，incoming 的第一层键是接收者，方便从两个方向查找和清理。
local outgoing = {}
local incoming = {}

local function now()
    return cs.server.info().ticked_time
end

local function remove_request(request)
    if outgoing[request.sender_id] == request then
        outgoing[request.sender_id] = nil
    end

    local requests = incoming[request.target_id]
    if requests ~= nil then
        requests[request.sender_id] = nil
        if next(requests) == nil then incoming[request.target_id] = nil end
    end
end

local function notify_online(steam_id, message)
    local player = cs.players.get_steamid(steam_id)
    if player ~= nil then player:print_chat(message) end
end

local function expire_request(request)
    -- 玩家可能取消请求后又发送了新请求，因此定时器必须确认自己仍对应当前请求。
    if outgoing[request.sender_id] ~= request then return end

    remove_request(request)
    notify_online(request.sender_id, "向 " .. request.target_name .. " 发送的传送请求已过期。")
    notify_online(request.target_id, request.sender_name .. " 的传送请求已过期。")
end

local function find_one_player(query, command)
    local matches = cs.players.find(query)
    if #matches == 0 then
        command:reply("没有找到玩家：" .. query)
        return nil
    end
    if #matches > 1 then
        command:reply("匹配到多名玩家，请输入更完整的名字、槽位、userid 或 SteamID64。")
        return nil
    end
    return matches[1]
end

local function current_requests(target_id)
    local result = {}
    local requests = incoming[target_id]
    if requests == nil then return result end

    for _, request in pairs(requests) do
        if request.expires_at <= now() then
            expire_request(request)
        else
            result[#result + 1] = request
        end
    end
    return result
end

local function select_request(player, query, command)
    local requests = current_requests(player.steam_id)
    if #requests == 0 then
        command:reply("当前没有等待处理的传送请求。")
        return nil
    end

    if query == nil then
        -- 与常见 TPA 体验一致：不写玩家名时直接处理最新收到的有效请求。
        local latest = requests[1]
        for index = 2, #requests do
            if requests[index].id > latest.id then latest = requests[index] end
        end
        return latest
    end

    local sender = find_one_player(query, command)
    if sender == nil then return nil end

    local request = outgoing[sender.steam_id]
    if request == nil or request.target_id ~= player.steam_id then
        command:reply(sender.name .. " 没有向你发送传送请求。")
        return nil
    end
    return request
end

plugin:command("css_tpa", {
    description = "请求传送到另一名玩家身边",
    allow_console = false,
    min_args = 1,
    usage = "<玩家>"
}, function(player, command)
    local target = find_one_player(command.args[1], command)
    if target == nil then return end
    if target.steam_id == player.steam_id then
        command:reply("不能向自己发送传送请求。")
        return
    end
    -- 机器人和 HLTV 无法主动执行接受命令，因此不允许把请求发给它们。
    if target.is_bot or target.is_hltv then
        command:reply("不能向机器人或 HLTV 发送传送请求。")
        return
    end

    local old_request = outgoing[player.steam_id]
    if old_request ~= nil then
        if old_request.expires_at <= now() then
            expire_request(old_request)
        else
            command:reply("你已经向 " .. old_request.target_name .. " 发送过请求，请等待处理或使用 css_tpcancel 取消。")
            return
        end
    end

    next_request_id = next_request_id + 1
    local request = {
        id = next_request_id,
        sender_id = player.steam_id,
        sender_slot = player.slot,
        sender_name = player.name,
        target_id = target.steam_id,
        target_slot = target.slot,
        target_name = target.name,
        expires_at = now() + request_timeout
    }
    outgoing[request.sender_id] = request
    incoming[request.target_id] = incoming[request.target_id] or {}
    incoming[request.target_id][request.sender_id] = request

    command:reply("已向 " .. target.name .. " 发送传送请求，" .. request_timeout .. " 秒后过期。")
    target:print_chat(player.name .. " 请求传送到你身边。输入 !tpaccept 接受，或 !tpdeny 拒绝。")

    plugin:after(request_timeout, function()
        -- id 检查让旧定时器无法误删同一玩家后来创建的新请求。
        local current = outgoing[request.sender_id]
        if current ~= nil and current.id == request.id then expire_request(current) end
    end, { stop_on_map_change = false })
end)

plugin:command("css_tpaccept", {
    description = "接受一名玩家的传送请求",
    allow_console = false,
    usage = "[玩家]"
}, function(player, command)
    local request = select_request(player, command.args[1], command)
    if request == nil then return end

    -- 重新按 SteamID64 获取快照，确保请求期间没有离服或发生 slot 身份变化。
    local sender = cs.players.get_steamid(request.sender_id)
    local target = player:refresh()
    if sender == nil then
        remove_request(request)
        command:reply(request.sender_name .. " 已经离开服务器，请求已取消。")
        return
    end
    if target == nil or target.steam_id ~= request.target_id then
        return
    end
    if not sender.is_alive then
        command:reply(sender.name .. " 当前未存活，暂时无法传送。")
        return
    end
    if not target.is_alive or target.position == nil then
        command:reply("你当前未存活或位置无效，暂时无法接受传送。")
        return
    end

    -- 这是娱乐服的宽松传送：不检查阵营、回合/冻结状态、战斗状态、
    -- 导航网格、目标是否在空中或坐标是否处于地图边界内，只要求双方仍有有效位置。
    -- 清零速度可避免请求者把原来的跳跃或坠落动量一并带到目标点。
    if not sender:teleport(target.position, target.eye_angles, cs.vec3(0, 0, 0)) then
        command:reply("传送失败，请稍后重试。")
        return
    end

    remove_request(request)
    command:reply("已接受 " .. sender.name .. " 的传送请求。")
    sender:print_chat("传送请求已被 " .. target.name .. " 接受。")
end)

plugin:command("css_tpdeny", {
    description = "拒绝一名玩家的传送请求",
    allow_console = false,
    usage = "[玩家]"
}, function(player, command)
    local request = select_request(player, command.args[1], command)
    if request == nil then return end

    remove_request(request)
    command:reply("已拒绝 " .. request.sender_name .. " 的传送请求。")
    notify_online(request.sender_id, request.target_name .. " 拒绝了你的传送请求。")
end)

plugin:command("css_tpcancel", {
    description = "取消自己发出的传送请求",
    allow_console = false
}, function(player, command)
    local request = outgoing[player.steam_id]
    if request == nil then
        command:reply("你当前没有等待处理的传送请求。")
        return
    end

    remove_request(request)
    command:reply("已取消向 " .. request.target_name .. " 发送的传送请求。")
    notify_online(request.target_id, request.sender_name .. " 取消了传送请求。")
end)

plugin:listen("OnClientDisconnect", function(slot)
    -- 此时控制器可能已进入 Disconnecting，读取完整玩家快照会触发无效原生字段。
    -- 请求创建时同时保存了连接槽位，因此可以直接定位需要清理的请求；长期身份仍用 SteamID。
    local affected = {}
    for _, request in pairs(outgoing) do
        if request.sender_slot == slot or request.target_slot == slot then
            affected[#affected + 1] = request
        end
    end

    for _, request in ipairs(affected) do
        remove_request(request)
        if request.sender_slot == slot then
            notify_online(request.target_id, request.sender_name .. " 已离开服务器，传送请求已取消。")
        else
            notify_online(request.sender_id, request.target_name .. " 已离开服务器，传送请求已取消。")
        end
    end
end)
