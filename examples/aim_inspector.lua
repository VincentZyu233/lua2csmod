local plugin = cs.plugin({
    name = "准星目标检查",
    version = "1.0.0",
    description = "查看准星当前指向的玩家"
})

plugin:command("css_luaaim", {
    description = "显示准星所指玩家",
    allow_console = false
}, function(player, command)
    -- aim_target 返回调用时的新快照，不是 player 表里预先缓存的字段。
    local target = player:aim_target()
    if target == nil then
        command:reply("准星当前没有指向有效玩家。")
        return
    end

    command:reply(string.format(
        "%s | 队伍 %s | 生命 %s | SteamID64 %s",
        target.name,
        target.team,
        tostring(target.health),
        target.steam_id
    ))
end)
