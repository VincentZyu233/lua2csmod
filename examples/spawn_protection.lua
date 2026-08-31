local plugin = cs.plugin({
    name = "出生保护",
    version = "1.0.0",
    description = "出生后短暂获得额外生命和提示"
})

local protection_seconds = 5

plugin:on("player_spawn", function(event)
    local player = event.player
    if player == nil or player.is_bot then return cs.continue end

    -- 此示例只提高生命和护甲，并非真正无敌；仍然可以被高伤害击杀。
    player:set_health(150)
    player:set_armor(100)
    player:print_chat(cs.colors.green .. "你获得了 " .. protection_seconds .. " 秒出生保护。")

    plugin:after(protection_seconds, function()
        -- 用旧快照 refresh 会校验玩家身份，防止离服后的槽位被其他人复用。
        local current = player:refresh()
        if current ~= nil and current.is_alive and current.health > 100 then
            current:set_health(100)
            current:print_chat(cs.colors.default .. "出生保护已结束。")
        end
    end)

    return cs.continue
end)
