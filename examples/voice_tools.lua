local plugin = cs.plugin({
    name = "语音工具",
    version = "1.0.0",
    description = "管理玩家的 CounterStrikeSharp 语音标志"
})

local modes = {
    normal = cs.voice.normal,
    muted = cs.voice.muted,
    all = cs.voice.all,
    listen_all = cs.voice.listen_all,
    team = cs.voice.team,
    listen_team = cs.voice.listen_team
}

plugin:command("css_luavoice", {
    description = "设置目标玩家的语音模式",
    permission = "@css/root",
    min_args = 2,
    usage = "<目标> <normal|muted|all|listen_all|team|listen_team>"
}, function(caller, command)
    local flags = modes[command.args[2]:lower()]
    if flags == nil then
        command:reply("未知语音模式。")
        return
    end

    local changed = 0
    for _, target in ipairs(cs.players.target(command.args[1], caller)) do
        if caller == nil or caller:can_target(target) then
            if target:set_voice_flags(flags) then changed = changed + 1 end
        end
    end
    command:reply("已更新 " .. changed .. " 名玩家的语音模式。")
end)

-- cs.voice 中的值是位标志，也可以使用 `|` 组合，例如：
-- player:set_voice_flags(cs.voice.team | cs.voice.listen_team)
