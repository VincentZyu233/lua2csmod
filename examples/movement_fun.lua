local plugin = cs.plugin({
    name = "移动参数工具",
    version = "0.0.1",
    description = "批量调整玩家重力和受击速度倍率"
})

plugin:command("css_luamove", {
    description = "设置一组玩家的重力和速度倍率",
    permission = "@css/root",
    min_args = 1,
    usage = "<目标> [重力 0-10] [速度倍率 0-10]"
}, function(caller, command)
    -- 缺省值 1 表示恢复游戏默认倍率，宿主会把数值限制到 0..10。
    local gravity = tonumber(command.args[2]) or 1
    local velocity_modifier = tonumber(command.args[3]) or 1
    local changed = 0

    -- velocity_modifier 可能被游戏受击逻辑再次覆盖，适合临时娱乐玩法。
    for _, target in ipairs(cs.players.target(command.args[1], caller)) do
        if caller == nil or caller:can_target(target) then
            if target:set_gravity(gravity) and target:set_velocity_modifier(velocity_modifier) then
                changed = changed + 1
            end
        end
    end

    command:reply("已更新 " .. changed .. " 名玩家的移动参数。")
end)
