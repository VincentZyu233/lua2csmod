local plugin = cs.plugin({
    name = "聊天冷却示例",
    version = "0.0.1",
    description = "演示 Listener 参数和按玩家保存短期状态"
})

local last_message_at = {}
local cooldown_seconds = 3

plugin:listen("OnPlayerChat", function(player, message, team_chat)
    if player == nil or message:lower() ~= "!lua时间" then return end

    -- ticked_time 是服务器运行时间，适合计算短期冷却，不受地图计时重置影响。
    local now = cs.server.info().ticked_time
    local previous = last_message_at[player.steam_id] or -cooldown_seconds
    local remaining = cooldown_seconds - (now - previous)
    if remaining > 0 then
        player:print_chat(string.format("请等待 %.1f 秒后再试。", remaining))
        return
    end

    last_message_at[player.steam_id] = now
    local channel = team_chat and "队伍聊天" or "全局聊天"
    player:print_chat(string.format("当前地图时间 %.1f 秒，来自%s。", cs.server.info().current_time, channel))
end)
