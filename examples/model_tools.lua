local plugin = cs.plugin({
    name = "模型与颜色工具",
    version = "0.0.1",
    description = "为玩家设置已知模型路径或渲染颜色"
})

plugin:command("css_luacolor", {
    description = "设置一组玩家的渲染颜色",
    permission = "@css/root",
    min_args = 4,
    usage = "<目标> <红> <绿> <蓝> [透明度]"
}, function(caller, command)
    local red = tonumber(command.args[2]) or 255
    local green = tonumber(command.args[3]) or 255
    local blue = tonumber(command.args[4]) or 255
    local alpha = tonumber(command.args[5]) or 255
    local changed = 0

    -- 颜色通道会被宿主限制到 0..255，目标选择沿用 CSS 原生语法。
    for _, target in ipairs(cs.players.target(command.args[1], caller)) do
        if caller == nil or caller:can_target(target) then
            if target:set_render_color(red, green, blue, alpha) then changed = changed + 1 end
        end
    end
    command:reply("已更新 " .. changed .. " 名玩家的颜色。")
end)

plugin:command("css_luamodel", {
    description = "设置一组玩家的模型",
    permission = "@css/root",
    min_args = 2,
    usage = "<目标> <已知有效的 .vmdl 路径>"
}, function(caller, command)
    local changed = 0
    -- 模型路径必须是服务器已知的 .vmdl；无效资源可能影响客户端稳定性。
    for _, target in ipairs(cs.players.target(command.args[1], caller)) do
        if caller == nil or caller:can_target(target) then
            if target:set_model(command.args[2], true) then changed = changed + 1 end
        end
    end
    command:reply("已更新 " .. changed .. " 名玩家的模型。")
end)
