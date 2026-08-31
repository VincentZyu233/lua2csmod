local text = {}

function text.welcome(name)
    -- 模块只返回普通 Lua 表，不会调用 cs.plugin，也不会被独立加载。
    return "欢迎，" .. name .. "！这条消息来自公共 Lua 模块。"
end

return text
