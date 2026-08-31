local plugin = cs.plugin({
    name = "帧与 Tick 调度示例",
    version = "1.0.0",
    description = "演示下一帧、下一次世界更新和延迟 Tick 回调"
})

plugin:command("css_frames", function(player, command)
    command:reply("已安排三个短期回调，请查看服务器控制台。")

    -- 下一游戏帧执行。服务器休眠时没有游戏帧，因此可能暂时不运行。
    plugin:next_frame(function()
        cs.log.info("next_frame 回调已执行")
    end)

    -- 下一次世界更新执行。文件热重载也使用这一机制回到游戏线程。
    plugin:next_world_update(function()
        cs.log.info("next_world_update 回调已执行")
    end)

    -- 在指定服务器 Tick 执行，参数至少为 1。按 64 Tick 估算，64 约为一秒。
    plugin:after_ticks(64, function()
        cs.log.info("after_ticks(64) 回调已执行")
    end)
end)

-- 调度注册会返回 ID，可在执行前取消；插件卸载或热重载也会自动使旧回调失效。
local cancelled = plugin:after_ticks(128, function()
    cs.log.error("这条日志不应出现")
end)
plugin:cancel(cancelled)
