local plugin = cs.plugin({
    name = "回合装备",
    version = "1.0.0",
    description = "每回合向存活玩家发放统一装备"
})

plugin:on("round_start", function()
    -- 稍后发放装备，避开游戏模式在 round_start 后继续执行的默认发枪流程。
    plugin:after(1, function()
        for _, player in ipairs(cs.players.humans()) do
            if player.is_alive then
                -- remove_weapons 会移除全部武器，因此需要重新补回刀具。
                player:remove_weapons()
                player:give_item("weapon_knife")
                player:give_item("weapon_ak47")
                player:give_item("weapon_deagle")
                player:set_armor(100)
            end
        end
    end)
    return cs.continue
end)
