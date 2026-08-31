local plugin = cs.plugin({
    name = "游戏状态",
    version = "1.0.0",
    description = "查询当前回合阶段和双方比分"
})

plugin:command("css_luagame", function(_, command)
    local rules = cs.game.rules()
    if rules == nil then
        command:reply("当前无法获取游戏规则实体。")
        return
    end

    command:reply(string.format(
        "回合 %d | T %s : %s CT | 热身 %s | 冻结时间 %s | C4 已安装 %s",
        rules.total_rounds_played,
        tostring(rules.terrorist_score or 0),
        tostring(rules.ct_score or 0),
        tostring(rules.warmup_period),
        tostring(rules.freeze_period),
        tostring(rules.bomb_planted)
    ))
end)
