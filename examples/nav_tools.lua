local plugin = cs.plugin({
    name = "导航网格工具",
    version = "0.0.1",
    description = "查询玩家脚下最近的 CS2 导航区域"
})

plugin:command("css_luanav", {
    description = "查看最近的导航区域",
    allow_console = false
}, function(player, command)
    -- 命令参数中的 player 是进入回调时的快照，refresh 可再次读取最新坐标。
    local current = player:refresh()
    if current == nil or current.position == nil then
        command:reply("当前无法读取玩家坐标。")
        return
    end

    -- 限制查询半径，避免把很远的导航区域误认为玩家脚下区域。
    local area = cs.nav.closest(current.position, 512)
    if area == nil then
        command:reply("512 单位内没有可用导航区域。")
        return
    end

    command:reply(string.format(
        "导航区域 #%d，距离 %.1f，大小 %.1f x %.1f",
        area.id,
        area.distance,
        area.width,
        area.height
    ))
end)
