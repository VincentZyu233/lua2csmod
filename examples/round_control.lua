local plugin = cs.plugin({
    name = "回合控制工具",
    version = "1.0.0",
    description = "由管理员结束当前回合"
})

plugin:command("css_luaendround", {
    description = "结束当前回合",
    permission = "@css/root",
    min_args = 1,
    usage = "<ct|t|draw> [延迟秒数]"
}, function(_, command)
    local reasons = {
        ct = cs.round_end.ct_win,
        t = cs.round_end.terrorist_win,
        draw = cs.round_end.draw
    }
    local reason = reasons[command.args[1]:lower()]
    if reason == nil then
        command:reply("原因必须是 ct、t 或 draw。")
        return
    end

    local delay = tonumber(command.args[2]) or 1
    if cs.game.terminate_round(delay, reason) then
        command:reply("已提交回合结束请求。")
    else
        command:reply("当前无法获取游戏规则实体。")
    end
end)
