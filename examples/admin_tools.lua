local plugin = cs.plugin({
    name = "Lua 管理工具",
    version = "1.0.0",
    description = "演示玩家查找、权限检查和常用管理操作"
})

local function find_one(query, caller, command)
    local matches = cs.players.find(query)
    if #matches == 0 then
        command:reply("没有找到玩家：" .. query)
        return nil
    end
    if #matches > 1 then
        command:reply("匹配到多名玩家，请输入更完整的名字、槽位或 userid。")
        return nil
    end
    if caller ~= nil and not caller:can_target(matches[1]) then
        command:reply("你的管理权限不足以操作该玩家。")
        return nil
    end
    return matches[1]
end

plugin:command("css_luaheal", {
    description = "设置玩家的生命值和护甲",
    permission = "@css/root",
    min_args = 1,
    usage = "<玩家> [生命值] [护甲]"
}, function(caller, command)
    local target = find_one(command.args[1], caller, command)
    if target == nil then return end

    local health = tonumber(command.args[2]) or 100
    local armor = tonumber(command.args[3]) or 100
    target:set_health(health)
    target:set_armor(armor)
    target:print_chat("管理员已将你的状态设置为 " .. health .. " 生命和 " .. armor .. " 护甲。")
    command:reply("已更新 " .. target.name .. " 的状态。")
end)

plugin:command("css_luagive", {
    description = "向玩家发放武器或物品",
    permission = "@css/root",
    min_args = 2,
    usage = "<玩家> <weapon_名称>"
}, function(caller, command)
    local target = find_one(command.args[1], caller, command)
    if target == nil then return end

    if target:give_item(command.args[2]) then
        command:reply("已向 " .. target.name .. " 发放 " .. command.args[2] .. "。")
    else
        command:reply("发放失败，请检查物品名称和玩家状态。")
    end
end)

plugin:command("css_luateam", {
    description = "切换玩家队伍并保留当前状态",
    permission = "@css/root",
    min_args = 2,
    usage = "<玩家> <t|ct|spec>"
}, function(caller, command)
    local target = find_one(command.args[1], caller, command)
    if target == nil then return end

    target:switch_team(command.args[2])
    command:reply("已切换 " .. target.name .. " 的队伍。")
end)
