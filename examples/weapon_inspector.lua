local plugin = cs.plugin({
    name = "武器检查器",
    version = "1.0.0",
    description = "显示玩家持有武器的详细快照"
})

plugin:command("css_luaweapons", {
    description = "查看自己或唯一匹配玩家的武器信息"
}, function(caller, command)
    local target = caller
    if command.args[1] ~= nil then
        local matches = cs.players.find(command.args[1])
        target = #matches == 1 and matches[1] or nil
    end
    if target == nil then
        command:reply("请在游戏内执行，或提供唯一匹配的玩家。")
        return
    end

    -- weapon_details 与 weapons 的数组索引一一对应，但包含完整句柄和弹药。
    command:reply(target.name .. " 持有 " .. #target.weapon_details .. " 件武器：")
    for _, weapon in ipairs(target.weapon_details) do
        command:reply(string.format(
            "#%d %s，弹匣 %d，备弹 %d，物品定义 %d",
            weapon.index,
            weapon.designer_name,
            weapon.clip,
            weapon.reserve,
            weapon.item_definition_index
        ))
    end
end)
