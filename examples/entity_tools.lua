local plugin = cs.plugin({
    name = "实体工具",
    version = "0.0.1",
    description = "查询实体并向实体发送 Source 2 I/O 输入"
})

plugin:command("css_luaentities", {
    description = "查询指定 Designer Name 的实体",
    permission = "@css/root",
    min_args = 1,
    usage = "<designer_name>"
}, function(_, command)
    -- 实体枚举可能有开销，示例主动限制最多读取 32 个。
    local entities = cs.entities.find(command.args[1], 32)
    command:reply("找到 " .. #entities .. " 个实体。")
    for index = 1, math.min(#entities, 5) do
        local entity = entities[index]
        command:reply(string.format(
            "#%d handle=%d name=%s designer=%s health=%s",
            entity.index,
            entity.handle,
            entity.name or "",
            entity.designer_name or "",
            tostring(entity.health)
        ))
    end
end)

plugin:command("css_luainput", {
    description = "向匹配实体发送 I/O 输入",
    permission = "@css/root",
    min_args = 2,
    usage = "<designer_name> <input> [value]"
}, function(_, command)
    local entities = cs.entities.find(command.args[1], 32)
    local changed = 0
    -- Source 2 I/O 会直接改变地图逻辑，只应向可信管理员开放此命令。
    for _, entity in ipairs(entities) do
        if entity:input(command.args[2], command.args[3] or "") then
            changed = changed + 1
        end
    end
    command:reply("已向 " .. changed .. " 个实体发送输入。")
end)
