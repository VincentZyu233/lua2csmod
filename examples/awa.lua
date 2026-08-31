local plugin = cs.plugin({
    name = "awa",
    version = "0.0.1",
    description = "玩家发送 awa 时回复"
})

plugin:on("player_chat", function(event)
    -- player_chat 是游戏事件；返回 cs.continue 保持其他插件和原事件继续处理。
    if event.player ~= nil and event.text:lower() == "awa" then
        event.player:print_chat(cs.colors.green .. "awa!" .. cs.colors.default)
    end

    return cs.continue
end)

plugin:on("player_connect_full", function(event)
    -- 过滤机器人，避免批量加 Bot 时刷屏。
    if event.player ~= nil and not event.player.is_bot then
        cs.server.print_chat_all(cs.colors.green .. "awa!!!" .. cs.colors.default)
    end

    return cs.continue
end)
