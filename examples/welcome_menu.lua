local plugin = cs.plugin({
    name = "入服欢迎菜单",
    version = "0.0.1",
    description = "玩家进入服务器后显示可选择的欢迎菜单"
})

plugin:listen("OnClientPutInServer", function(slot)
    local joined = cs.players.get(slot)
    if joined == nil then return end

    -- 延迟打开菜单，确保玩家控制器和客户端界面均已准备好。
    plugin:after(2, function()
        local player = joined:refresh()
        if player == nil or player.is_bot then return end

        cs.menu.open(player, {
            title = "欢迎来到 Lua2CS 私服",
            post_select = "close",
            items = {
                { text = "查看服务器状态" },
                { text = "获得一把 Deagle" },
                { text = "稍后再说" }
            }
        }, function(selected, index)
            if index == 1 then
                local info = cs.server.info()
                selected:print_chat(string.format("地图 %s，在线 %d 人。", info.map_name, cs.players.count()))
            elseif index == 2 then
                selected:give_item("weapon_deagle")
            end
        end)
    end)
end)
