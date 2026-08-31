local plugin = cs.plugin({
    name = "回合伤害统计",
    version = "0.0.1",
    description = "统计每名玩家在当前回合造成的有效伤害"
})

local damage = {}

plugin:on("round_start", function()
    -- 回合开始时重建表，避免上一回合的数据混入。
    damage = {}
    return cs.continue
end)

plugin:on("player_hurt", function(event)
    local attacker = event.attacker
    local victim = event.player
    if attacker == nil or victim == nil or attacker.slot == victim.slot then
        return cs.continue
    end

    local amount = tonumber(event.dmg_health) or 0
    damage[attacker.steam_id] = (damage[attacker.steam_id] or 0) + amount
    return cs.continue
end)

plugin:on("round_end", function()
    -- 延迟一小段时间发送，避免与游戏自己的回合结束提示挤在一起。
    plugin:after(0.5, function()
        for _, player in ipairs(cs.players.humans()) do
            player:print_chat("本回合造成伤害：" .. (damage[player.steam_id] or 0))
        end
    end)
    return cs.continue
end)
