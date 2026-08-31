local plugin = cs.plugin({
    name = "击杀奖励",
    version = "0.0.1",
    description = "击杀敌人后增加金钱并显示提示"
})

local reward = 500

plugin:on("player_death", function(event)
    local attacker = event.attacker
    local victim = event.player
    -- 世界伤害没有 attacker，自杀时攻击者和受害者是同一槽位。
    if attacker == nil or victim == nil or attacker.slot == victim.slot then
        return cs.continue
    end

    -- 金钱可能在无有效服务时为 nil，因此提供 0 作为回退值。
    attacker:set_money((attacker.money or 0) + reward)
    attacker:print_center("击杀奖励 +$" .. reward)
    return cs.continue
end)
