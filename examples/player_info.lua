local plugin = cs.plugin({
    name = "玩家信息",
    version = "1.0.0",
    description = "查询玩家快照和武器列表"
})

plugin:command("css_luainfo", {
    description = "查看自己或指定玩家的信息"
}, function(caller, command)
    local target = caller
    -- 提供参数时要求唯一匹配，避免名字片段同时命中多人。
    if command.args[1] ~= nil then
        local matches = cs.players.find(command.args[1])
        target = #matches == 1 and matches[1] or nil
    end
    if target == nil then
        command:reply("请在游戏内执行，或提供唯一匹配的玩家。")
        return
    end

    command:reply(string.format(
        "%s | SteamID64 %s | 队伍 %s | 生命 %s | 护甲 %s | 金钱 %s | 延迟 %s",
        target.name,
        target.steam_id,
        target.team,
        tostring(target.health),
        tostring(target.armor),
        tostring(target.money),
        tostring(target.ping)
    ))
    -- weapons 是简化名称数组；需要弹药和句柄时可改读 weapon_details。
    command:reply("武器：" .. (#target.weapons > 0 and table.concat(target.weapons, "、") or "无"))
end)
