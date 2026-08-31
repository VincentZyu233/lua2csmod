local text = require("modules.text")

local plugin = cs.plugin({
    name = "模块化示例",
    version = "1.0.0",
    description = "从子目录加载公共 Lua 模块"
})

plugin:command("css_luawelcome", function(player, command)
    local name = player ~= nil and player.name or "控制台"
    command:reply(text.welcome(name))
end)
