local plugin = cs.plugin({
    name = "Lua 菜单示例",
    version = "0.0.1",
    description = "展示聊天菜单和选项回调"
})

-- 文本与索引分开保存，使菜单回调可以复用同一份显示名称。
local choices = {
    "补满生命",
    "获得 AK-47",
    "恢复默认重力"
}

plugin:command("css_luamenu", {
    description = "打开 Lua 功能菜单",
    allow_console = false
}, function(player)
    -- 每个玩家同一时间只有一个 CSS 菜单，新菜单会替换旧菜单。
    cs.menu.open(player, {
        title = "Lua 娱乐菜单",
        type = "chat",
        exit_button = true,
        post_select = "close",
        items = {
            { text = choices[1] },
            { text = choices[2] },
            { text = choices[3] }
        }
    }, function(selected_player, index)
        -- selected_player 是点击瞬间生成的新快照，不必再手动 refresh。
        if index == 1 then
            selected_player:set_health(selected_player.max_health or 100)
        elseif index == 2 then
            selected_player:give_item("weapon_ak47")
        elseif index == 3 then
            selected_player:set_gravity(1)
        end
        selected_player:print_chat("已选择：" .. choices[index])
    end)
end)
