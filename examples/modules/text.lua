local text = {}

function text.welcome(name)
    return "欢迎，" .. name .. "！这条消息来自公共 Lua 模块。"
end

return text
