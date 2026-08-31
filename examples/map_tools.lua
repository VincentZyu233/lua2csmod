local plugin = cs.plugin({
    name = "地图工具",
    version = "1.0.0",
    description = "查询服务器信息并安全切换地图"
})

plugin:command("css_luaserver", function(_, command)
    local info = cs.server.info()
    command:reply(string.format(
        "地图：%s，玩家：%d，Tick：%d，地图时间：%.1f 秒",
        info.map_name,
        cs.players.count(),
        info.tick_count,
        info.current_time
    ))
end)

plugin:command("css_luamap", {
    description = "切换到指定地图",
    permission = "@css/root",
    min_args = 1,
    usage = "<地图名>"
}, function(_, command)
    local map = command.args[1]
    if not cs.server.is_map_valid(map) then
        command:reply("地图不存在或不可用：" .. map)
        return
    end
    command:reply("正在切换到 " .. map .. "。")
    cs.server.execute("changelevel " .. map)
end)
