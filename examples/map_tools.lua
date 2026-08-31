local plugin = cs.plugin({
    name = "地图工具",
    version = "0.0.1",
    description = "查询服务器信息并安全切换地图"
})

plugin:command("css_luaserver", function(_, command)
    -- server.info 一次返回地图、Tick 和多种时间，适合状态面板统一读取。
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
    -- 在拼接 changelevel 命令前先让服务器验证地图名，避免无效输入进入命令。
    if not cs.server.is_map_valid(map) then
        command:reply("地图不存在或不可用：" .. map)
        return
    end
    command:reply("正在切换到 " .. map .. "。")
    cs.server.execute("changelevel " .. map)
end)
