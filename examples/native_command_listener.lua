local plugin = cs.plugin({
    name = "原生命令监听示例",
    version = "1.0.0",
    description = "演示在 CS2 原生命令执行前后观察或拦截命令"
})

-- pre 在引擎处理命令前执行。返回 cs.handled 会阻止原始行为，但仍允许后续 Hook；
-- 返回 cs.stop 会同时阻止原始行为和后续 Hook，通常只在明确需要独占拦截时使用。
plugin:command_listener("kill", { mode = "pre" }, function(player, command)
    if player == nil then return cs.continue end
    player:print_chat("本服已禁用控制台自杀命令。")
    return cs.handled
end)

-- post 适合审计或响应已完成的动作。命令快照与 plugin:command 的 command 参数一致，
-- 但监听器的返回值只有在对应 HookMode 下由 CSS/游戏引擎决定是否有实际作用。
plugin:command_listener("say", { mode = "post" }, function(player, command)
    if player ~= nil then
        cs.log.debug(string.format("玩家 %s 执行了 %s，参数：%s", player.name, command.name, command.arg_string))
    end
    return cs.continue
end)
