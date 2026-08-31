local plugin = cs.plugin({
    name = "持久化击杀统计",
    version = "1.0.0",
    description = "跨热重载保存玩家累计击杀数"
})

local function storage_key(steam_id)
    return "kills:" .. steam_id
end

plugin:on("player_death", function(event)
    local attacker = event.attacker
    local victim = event.player
    if attacker == nil or victim == nil or attacker.slot == victim.slot or attacker.is_bot then
        return cs.continue
    end

    local key = storage_key(attacker.steam_id)
    local kills = cs.storage.get(key, 0) + 1
    cs.storage.set(key, kills)
    attacker:print_chat("你的 Lua 累计击杀数：" .. kills)
    return cs.continue
end)

plugin:command("css_luakills", {
    description = "查看自己的 Lua 累计击杀数",
    allow_console = false
}, function(player, command)
    command:reply("累计击杀：" .. cs.storage.get(storage_key(player.steam_id), 0))
end)
