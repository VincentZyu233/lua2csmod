local plugin = cs.plugin({
    name = "无限丢枪",
    version = "1.0.0",
    description = "按 Q 正常丢出枪械，并在下一帧向玩家补发等价副本"
})

-- 世界里的复制武器会占用实体数量。这里设置自动清理时间，避免玩家持续按 Q
-- 最终堆积大量武器。需要永久保留时可以删除 remove 调用，但不建议这样做。
local cleanup_seconds = 30
local cooldown_seconds = 0.25
local last_drop = {}

-- 刀、C4、手雷和医疗针有特殊的游戏逻辑，不适合按普通枪械直接复制。
-- 这个模板默认只处理常见枪械；若要放开特殊物品，应逐个在真实服务器测试。
local excluded = {
    weapon_knife = true,
    weapon_knife_t = true,
    weapon_c4 = true,
    weapon_taser = true,
    weapon_healthshot = true,
    weapon_flashbang = true,
    weapon_hegrenade = true,
    weapon_smokegrenade = true,
    weapon_molotov = true,
    weapon_incgrenade = true,
    weapon_decoy = true
}

plugin:command_listener("drop", { mode = "pre" }, function(player, command)
    -- 控制台也可能执行 drop；没有玩家或没有有效 Pawn 时交回原生命令处理。
    if player == nil or not player.is_alive then
        return cs.continue
    end

    local weapon = player.active_weapon_info
    if weapon == nil or excluded[weapon.designer_name] then
        return cs.continue
    end

    -- 使用 SteamID 做冷却键，避免槽位被新玩家复用后继承旧状态。
    local now = cs.server.info().engine_time
    local previous = last_drop[player.steam_id] or -1000
    if now - previous < cooldown_seconds then
        return cs.handled
    end
    last_drop[player.steam_id] = now

    -- 这里只复制弹药和可见经济属性，不复制 item_id/account_id 等库存身份字段。
    local options = {
        clip = weapon.clip,
        clip_secondary = weapon.clip_secondary,
        reserve = weapon.reserve,
        reserve_secondary = weapon.reserve_secondary,
        paint_kit = weapon.paint_kit,
        paint_seed = weapon.paint_seed,
        paint_wear = weapon.paint_wear,
        stattrak = weapon.stattrak,
        entity_quality = weapon.entity_quality,
        custom_name = weapon.custom_name,
        custom_name_override = weapon.custom_name_override,
        original_owner_steam_id = weapon.original_owner_steam_id
    }

    local steam_id = player.steam_id
    local user_id = player.user_id
    plugin:next_frame(function()
        -- 先让引擎执行原生 drop，地上的武器由 CS2 自己创建和管理；下一帧再补回
        -- 同类武器。这样不依赖不稳定的手工世界武器创建，玩家只会短暂空手一帧。
        local current = cs.players.get_steamid(steam_id)
        if current == nil or current.user_id ~= user_id then return end
        local replacement = current:give_weapon(weapon.designer_name, options)
        if replacement == nil then
            current:print_chat("武器副本补发失败，本次按普通丢枪处理。")
        end

        -- weapon 仍指向刚刚由引擎丢到地上的原实体。定时删除可避免无限堆积；
        -- 若期间被其他玩家捡走，也会在到期时一并删除，可按服务器玩法调整时长。
        plugin:after(cleanup_seconds, function()
            weapon:remove()
        end)
    end)
    return cs.continue
end)

plugin:listen("OnClientDisconnect", function(slot)
    -- 离开时清理冷却记录。Listener 参数是槽位，因此重新查询可能已经得到 nil。
    local player = cs.players.get(slot)
    if player ~= nil then last_drop[player.steam_id] = nil end
end)
