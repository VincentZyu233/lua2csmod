local plugin = cs.plugin({
    name = "传送点",
    version = "0.0.1",
    description = "保存当前位置并随时传送返回"
})

-- 这里只保存在 Lua VM 内存中，热重载后会清空；跨重载请改用 cs.storage。
local checkpoints = {}

plugin:command("css_savepos", {
    description = "保存当前位置",
    allow_console = false
}, function(player, command)
    if not player.is_alive or player.position == nil then
        command:reply("只有存活玩家可以保存位置。")
        return
    end
    -- 使用 SteamID64 作为键，不依赖会被复用的玩家槽位。
    checkpoints[player.steam_id] = {
        position = player.position,
        angles = player.eye_angles
    }
    command:reply("当前位置已保存。")
end)

plugin:command("css_backpos", {
    description = "返回已保存的位置",
    allow_console = false
}, function(player, command)
    local checkpoint = checkpoints[player.steam_id]
    if checkpoint == nil then
        command:reply("你还没有保存位置。")
        return
    end
    -- 把速度清零，避免传送后继续保留跳跃或坠落动量。
    if not player:teleport(checkpoint.position, checkpoint.angles, cs.vec3(0, 0, 0)) then
        command:reply("传送失败，请确认当前玩家状态。")
    end
end)
