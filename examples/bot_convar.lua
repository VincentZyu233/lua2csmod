local plugin = cs.plugin({
    name = "机器人 ConVar 工具",
    version = "0.0.1",
    description = "修改机器人报告的客户端 ConVar"
})

plugin:command("css_luabotcvar", {
    description = "设置所有机器人的客户端 ConVar",
    permission = "@css/root",
    min_args = 2,
    usage = "<ConVar> <值>"
}, function(_, command)
    local changed = 0
    for _, bot in ipairs(cs.players.bots()) do
        -- CSS 的 SetFakeClientConVar 只允许对机器人调用，真人玩家会被接口拒绝。
        if bot:set_fake_convar(command.args[1], command.args[2]) then
            changed = changed + 1
        end
    end
    command:reply("已更新 " .. changed .. " 个机器人。")
end)
