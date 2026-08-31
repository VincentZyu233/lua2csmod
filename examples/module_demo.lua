-- require 只从当前脚本目录加载 Lua 模块，子目录文件不会独立成为插件。
local text = require("modules.text")

local plugin = cs.plugin({
    name = "模块化示例",
    version = "0.0.1",
    description = "从子目录加载公共 Lua 模块"
})

plugin:command("css_luawelcome", function(player, command)
    -- 服务器控制台也能执行命令，此时 player 为 nil。
    local name = player ~= nil and player.name or "控制台"
    command:reply(text.welcome(name))
end)
