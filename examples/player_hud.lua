local plugin = cs.plugin({
    name = "玩家状态 HUD",
    version = "1.0.0",
    description = "定时显示生命、护甲、金钱和当前武器"
})

plugin:every(1, function()
    for _, player in ipairs(cs.players.humans()) do
        if player.is_alive then
            local html = string.format(
                "<font color='#7CFC00'>生命 %d</font>　护甲 %d　金钱 $%d<br><font color='#E8E8E8'>%s</font>",
                player.health or 0,
                player.armor or 0,
                player.money or 0,
                player.active_weapon or "无武器"
            )
            player:print_html(html, 2)
        end
    end
end)
