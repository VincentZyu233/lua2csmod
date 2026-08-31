local plugin = cs.plugin({
    name = "Round Timer",
    version = "1.0.0"
})

plugin:on("round_start", function(event)
    cs.server.print_chat_all("Round started: " .. tostring(event.objective))
    return cs.continue
end)

plugin:timer(60, function()
    cs.server.print_chat_all("This server is running Lua2CS.")
end, {
    repeating = true,
    stop_on_map_change = true
})
