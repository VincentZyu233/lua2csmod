local plugin = cs.plugin({
    name = "武器工坊示例",
    version = "0.0.1",
    description = "演示发放和复制武器，以及修改弹药和外观属性"
})

plugin:command("css_luaweapon", {
    description = "给自己一把带自定义弹药的 AK-47",
    permission = "@css/generic",
    allow_console = false
}, function(player, command)
    if player == nil then return end

    -- give_weapon 返回武器对象，失败返回 nil；旧的 give_item 仍保持布尔返回值。
    local weapon = player:give_weapon("weapon_ak47", {
        clip = 35,
        reserve = 120,
        paint_kit = 0,
        paint_wear = 0.01,
        custom_name = "Lua 测试武器"
    })
    if weapon == nil then
        command:reply("武器发放失败。")
        return
    end

    -- 武器对象也是短期快照。需要最新弹药或位置时调用 refresh，并检查 nil。
    local latest = weapon:refresh()
    command:reply(string.format(
        "已发放 %s，实体 #%d，弹匣 %d，备弹 %d",
        latest.designer_name,
        latest.index,
        latest.clip,
        latest.reserve
    ))
end)

plugin:command("css_copyweapon", {
    description = "复制当前武器并发放到自己的背包",
    permission = "@css/generic",
    allow_console = false
}, function(player, command)
    if player == nil or player.active_weapon_info == nil then return end
    local source = player.active_weapon_info
    local copy = player:give_weapon(source.designer_name, {
        clip = source.clip,
        reserve = source.reserve,
        paint_kit = source.paint_kit,
        paint_seed = source.paint_seed,
        paint_wear = source.paint_wear,
        stattrak = source.stattrak,
        custom_name = source.custom_name
    })
    if copy == nil then
        command:reply("当前武器复制失败。")
        return
    end
    command:reply("已复制当前武器；请注意武器槽位和游戏规则仍可能限制持有数量。")
end)
