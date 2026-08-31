local plugin = cs.plugin({
    name = "弹药补给",
    version = "1.0.0",
    description = "查看并补充当前武器的弹匣和备弹"
})

plugin:command("css_luaammo", {
    description = "补充当前武器弹药",
    allow_console = false,
    usage = "[弹匣数量] [备弹数量]"
}, function(player, command)
    -- 玩家快照中的 active_weapon_info 是执行命令这一刻的数据。
    -- 修改成功后如需读取新数量，应再次调用 player:refresh()。
    local weapon = player.active_weapon_info
    if weapon == nil then
        command:reply("当前没有可修改弹药的武器。")
        return
    end

    local clip = tonumber(command.args[1]) or 30
    local reserve = tonumber(command.args[2]) or 90
    if player:set_ammo(clip, reserve) then
        command:reply(string.format(
            "%s：弹匣 %d -> %d，备弹 %d -> %d。",
            weapon.designer_name,
            weapon.clip,
            clip,
            weapon.reserve,
            reserve
        ))
    else
        command:reply("弹药修改失败，武器可能已经切换或失效。")
    end
end)
