local plugin = cs.plugin({
    name = "计分板工具",
    version = "1.0.0",
    description = "由管理员修改玩家分数、回合分和 MVP 次数"
})

plugin:command("css_luascore", {
    description = "修改目标玩家的计分板数据",
    permission = "@css/root",
    min_args = 2,
    usage = "<目标> <总分> [回合分] [MVP]"
}, function(caller, command)
    local score = tonumber(command.args[2])
    local round_score = tonumber(command.args[3]) or score
    local mvps = tonumber(command.args[4])
    if score == nil then
        command:reply("总分必须是非负整数。")
        return
    end

    local changed = 0
    for _, target in ipairs(cs.players.target(command.args[1], caller)) do
        -- 控制台调用时 caller 为 nil，不需要进行免疫等级比较。
        if caller == nil or caller:can_target(target) then
            local ok = target:set_score(score) and target:set_round_score(round_score)
            if mvps ~= nil then ok = target:set_mvps(mvps) and ok end
            if ok then changed = changed + 1 end
        end
    end
    command:reply("已更新 " .. changed .. " 名玩家的计分板数据。")
end)
