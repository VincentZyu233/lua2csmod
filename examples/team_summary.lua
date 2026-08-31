local plugin = cs.plugin({
    name = "队伍人数统计",
    version = "0.0.1",
    description = "统计各队真人、机器人和存活人数"
})

plugin:command("css_luateams", function(_, command)
    local summary = {
        [cs.team.t] = { total = 0, alive = 0, bots = 0 },
        [cs.team.ct] = { total = 0, alive = 0, bots = 0 }
    }

    for _, player in ipairs(cs.players.all()) do
        local team = summary[player.team_id]
        if team ~= nil then
            team.total = team.total + 1
            if player.is_alive then team.alive = team.alive + 1 end
            if player.is_bot then team.bots = team.bots + 1 end
        end
    end

    -- 先按队伍 ID 聚合，最后再统一格式化，适合扩展更多统计字段。
    local t = summary[cs.team.t]
    local ct = summary[cs.team.ct]
    command:reply(string.format(
        "T：%d 人 / %d 存活 / %d 机器人；CT：%d 人 / %d 存活 / %d 机器人",
        t.total, t.alive, t.bots,
        ct.total, ct.alive, ct.bots
    ))
end)
