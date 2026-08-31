local plugin = cs.plugin({
    name = "进出服播报",
    version = "1.0.0",
    description = "使用 Listener 播报玩家加入和离开"
})

plugin:listen("OnClientAuthorized", function(slot, steam_id)
    plugin:after(1, function()
        local player = cs.players.get(slot)
        if player ~= nil then
            cs.server.print_chat_all(cs.colors.green .. player.name .. " 加入了服务器。")
            cs.log.info(player.name .. " 已通过 Steam 验证：" .. steam_id)
        end
    end)
end)

plugin:listen("OnClientDisconnect", function(slot)
    local player = cs.players.get(slot)
    if player ~= nil then
        cs.server.print_chat_all(cs.colors.yellow .. player.name .. " 离开了服务器。")
    end
end)
