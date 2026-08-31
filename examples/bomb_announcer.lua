local plugin = cs.plugin({
    name = "炸弹事件播报",
    version = "0.0.1",
    description = "用中文播报 C4 的关键状态变化"
})

local function player_name(player)
    return player ~= nil and player.name or "未知玩家"
end

plugin:on("bomb_planted", function(event)
    cs.server.print_chat_all(cs.colors.red .. player_name(event.player) .. " 已安装 C4！")
    return cs.continue
end)

plugin:on("bomb_defused", function(event)
    cs.server.print_chat_all(cs.colors.green .. player_name(event.player) .. " 已拆除 C4。")
    return cs.continue
end)

plugin:on("bomb_dropped", function(event)
    -- 事件玩家字段会由 Lua2CS 自动转换成玩家快照。
    cs.server.print_chat_all(cs.colors.yellow .. player_name(event.player) .. " 掉落了 C4。")
    return cs.continue
end)
