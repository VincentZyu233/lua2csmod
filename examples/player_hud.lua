local plugin = cs.plugin({
    name = "玩家状态 HUD",
    version = "0.0.1",
    description = "定时显示生命、护甲、金钱和当前武器"
})

-- 每秒刷新足够用于状态 HUD；不要在 OnTick 中给全部玩家反复生成 HTML。
plugin:every(1, function()
    for _, player in ipairs(cs.players.humans()) do
        if player.is_alive then
            -- HTML 来自固定模板，不要直接拼接未经处理的玩家输入。
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
