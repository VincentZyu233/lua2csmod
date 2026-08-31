local plugin = cs.plugin({
    name = "Hello Lua",
    version = "1.0.0",
    description = "Minimal Lua2CS example"
})

plugin:on_load(function(hot_reload)
    cs.log.info("Hello from Lua 5.4; hot reload = " .. tostring(hot_reload))
end)

plugin:command("css_luahello", {
    description = "Reply from a Lua plugin"
}, function(player, command)
    command:reply("Hello from Lua 5.4!")
end)
