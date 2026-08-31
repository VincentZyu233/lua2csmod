local plugin = cs.plugin({
    name = "客户端 ConVar 工具",
    version = "1.0.0",
    description = "向指定客户端复制服务器给出的 ConVar 值"
})

plugin:command("css_luareplicate", {
    description = "向目标客户端复制 ConVar 值",
    permission = "@css/root",
    min_args = 3,
    usage = "<目标> <ConVar> <值>"
}, function(caller, command)
    local changed = 0
    for _, target in ipairs(cs.players.target(command.args[1], caller)) do
        if caller == nil or caller:can_target(target) then
            if target:replicate_convar(command.args[2], command.args[3]) then
                changed = changed + 1
            end
        end
    end

    -- replicate_convar 只向客户端发送值，不会修改服务器自身的 ConVar。
    command:reply("已向 " .. changed .. " 名玩家复制 ConVar。")
end)
