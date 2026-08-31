local plugin = cs.plugin({
    name = "批量目标工具",
    version = "1.0.0",
    description = "使用 CounterStrikeSharp 原生目标语法批量操作玩家"
})

plugin:command("css_luaarmor", {
    description = "为一组目标设置护甲",
    permission = "@css/root",
    min_args = 1,
    usage = "<@all|@ct|@t|@alive|#userid|名字> [护甲]"
}, function(caller, command)
    local targets = cs.players.target(command.args[1], caller)
    local armor = tonumber(command.args[2]) or 100
    local changed = 0

    for _, target in ipairs(targets) do
        if caller == nil or caller:can_target(target) then
            if target:set_armor(armor) then changed = changed + 1 end
        end
    end

    command:reply("已更新 " .. changed .. " 名玩家的护甲。")
end)
