local block_definition = require("blocks")

function on_initialize()
    window.set_title("Squid Lilly")
    window.set_vsync(true)
    window.set_refresh_rate(70)

    camera.register_fps("test")
    camera.set_active("test")
end

input_manager.bind_key("F5", function()
    input_manager.toggle_mouse()
end)

input_manager.bind_key("F2", function()
    notifications.error("Test")
end)

input_manager.bind_key("F3", function()
    rendering.toggle_wireframe()

    local state = rendering.is_wireframe() and "enabled" or "disabled"
    notifications.info("Wireframe mode " .. state)
end)

input_manager.bind_key("GraveAccent", function()
    quake_console.toggle()
end)

input_manager.bind_mouse(function(xDelta, yDelta, posX, posY)
    -- FPS camera controls: mouse right = look right, mouse down = look down
    local sensitivity = 0.003
    camera.dispatch_mouse_fps(yDelta * sensitivity, xDelta * sensitivity)
end)

input_manager.bind_mouse_click(function(button, posX, posY)
    console.log("Mouse button " .. button .. " at: " .. posX .. ", " .. posY)

    if button == "left" then
        world.remove_block()
    end
end)

input_manager.bind_key_held("W", function()
    camera.dispatch_keyboard_fps(1, 0, 0)
end)

input_manager.bind_key_held("S", function()
    camera.dispatch_keyboard_fps(-1, 0, 0)
end)

input_manager.bind_key_held("A", function()
    camera.dispatch_keyboard_fps(0, 1, 0)
end)

input_manager.bind_key_held("D", function()
    camera.dispatch_keyboard_fps(0, -1, 0)
end)

input_manager.bind_key_held("Space", function()
    camera.dispatch_keyboard_fps(0, 0, 1)
end)

input_manager.bind_key("Escape", function()
    input_manager.release_mouse()
end)

engine.on_update(
    -- For Shift, check directly in update since binding doesn't work with modifier keys
    function(gt)
        if input_manager.is_key_down("ShiftLeft") or input_manager.is_key_down("ShiftRight") then
            camera.dispatch_keyboard_fps(0, 0, -1)
        end
    end
)

assets.load_texture("ground_texture", "textures/ground_texture.png")

assets.load_atlas("default", "textures/blocks_alternatives.png", 16, 16, 0, 0)
assets.load_atlas("default2", "textures/blocks.png", 16, 16, 0, 0)
assets.load_atlas("default3", "textures/block_map.png", 16, 16, 0, 0)
block_definition.load_tiles()

input_manager.grab_mouse()
