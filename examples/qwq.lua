local plugin = cs.plugin({
    name = "QwQ",
    version = "1.0.0",
    description = "Replies when a player says qwq"
})

plugin:on("player_chat", function(event)
    if event.player ~= nil and event.text:lower() == "qwq" then
        event.player:print_chat(cs.colors.green .. "qwq!" .. cs.colors.default)
    end

    return cs.continue
end)

plugin:on("player_connect_full", function(event)
    if event.player ~= nil and not event.player.is_bot then
        cs.server.print_chat_all(cs.colors.green .. "qwq!!!" .. cs.colors.default)
    end

    return cs.continue
end)
