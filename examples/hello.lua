local plugin = cs.plugin({
    name = "你好 Lua",
    version = "1.0.0",
    description = "最小 Lua2CS 示例"
})

plugin:on_load(function(hot_reload)
    -- 首次加载为 false，保存文件触发的替换式热重载为 true。
    cs.log.info("来自 Lua 5.4 的问候；是否热重载：" .. tostring(hot_reload))
end)

plugin:command("css_luahello", {
    description = "从 Lua 插件回复"
}, function(player, command)
    -- command:reply 会自动选择玩家聊天或服务器控制台作为回复位置。
    command:reply("你好，消息来自 Lua 5.4！")
end)
