local plugin = cs.plugin({
    name = "回合计时器",
    version = "1.0.0"
})

plugin:on("round_start", function(event)
    cs.server.print_chat_all("回合开始，目标：" .. tostring(event.objective))
    return cs.continue
end)

plugin:timer(60, function()
    cs.server.print_chat_all("此服务器正在运行 Lua2CS。")
end, {
    repeating = true,
    stop_on_map_change = true
})
