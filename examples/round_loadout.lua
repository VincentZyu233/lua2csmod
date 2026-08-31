local plugin = cs.plugin({
    name = "回合装备",
    version = "1.0.0",
    description = "每回合向存活玩家发放统一装备"
})

plugin:on("round_start", function()
    plugin:after(1, function()
        for _, player in ipairs(cs.players.humans()) do
            if player.is_alive then
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
