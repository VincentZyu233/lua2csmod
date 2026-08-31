local plugin = cs.plugin({
    name = "随机出生装备",
    version = "0.0.1",
    description = "每次出生时从预设武器中随机选择一把"
})

local primary_weapons = {
    "weapon_ak47",
    "weapon_m4a1_silencer",
    "weapon_awp",
    "weapon_ssg08"
}

plugin:on("player_spawn", function(event)
    local player = event.player
    if player == nil or player.is_bot then return cs.continue end

    -- 出生事件触发时 Pawn 已存在，但延迟一帧附近可减少与游戏发装备流程冲突。
    plugin:after(0.1, function()
        -- refresh 会同时校验 slot、userid 和 SteamID，避免槽位复用时操作到别人。
        local current = player:refresh()
        if current == nil or not current.is_alive then return end

        local weapon = primary_weapons[math.random(#primary_weapons)]
        current:remove_weapons()
        current:give_item("weapon_knife")
        current:give_item(weapon)
        current:give_item("weapon_deagle")
        current:print_chat("本次随机主武器：" .. weapon)
    end)
    return cs.continue
end)
