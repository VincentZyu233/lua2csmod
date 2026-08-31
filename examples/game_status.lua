local plugin = cs.plugin({
    name = "游戏状态",
    version = "0.0.1",
    description = "查询当前回合阶段和双方比分"
})

plugin:command("css_luagame", function(_, command)
    -- 游戏规则实体在地图加载早期可能尚未出现，因此必须处理 nil。
    local rules = cs.game.rules()
    if rules == nil then
        command:reply("当前无法获取游戏规则实体。")
        return
    end

    -- rules 是调用时的快照；长期保存不会自动更新。
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
