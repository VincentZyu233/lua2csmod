local plugin = cs.plugin({
    name = "击杀奖励",
    version = "1.0.0",
    description = "击杀敌人后增加金钱并显示提示"
})

local reward = 500

plugin:on("player_death", function(event)
    local attacker = event.attacker
    local victim = event.player
    if attacker == nil or victim == nil or attacker.slot == victim.slot then
        return cs.continue
    end

    attacker:set_money((attacker.money or 0) + reward)
    attacker:print_center("击杀奖励 +$" .. reward)
    return cs.continue
end)
