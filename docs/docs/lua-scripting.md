# Lua Scripting Guide

Lilly.Engine uses Lua (through MoonSharp) for game logic, allowing you to write and modify behavior without recompiling. This guide covers the actual API based on your engine's implementation.

## Why Lua?

**Fast Iteration** - Change game logic without rebuilding
**Modding** - Players can create mods with scripts
**Prototyping** - Test ideas quickly
**Hot Reload** - Scripts reload when files change
**Auto-generated definitions** - LSP autocomplete with `definitions.lua`

## Script Structure

The engine loads `scripts/init.lua` as the entry point. Here's the typical structure:

```lua
-- scripts/init.lua

-- Load modules
local blocks = require("blocks")

-- Initialization function
function on_initialize()
    window.set_title("My Game")
    window.set_vsync(true)

    -- Setup camera
    camera.register_fps("main")
    camera.set_active("main")

    -- Load assets
    assets.load_atlas("blocks", "textures/blocks.png", 16, 16, 0, 0)
    blocks.load_tiles()
end

-- Bind input
input_manager.bind_key("Escape", function()
    input_manager.release_mouse()
end)

-- Update callback
engine.on_update(function(game_time)
    -- Called every frame
    -- game_time.elapsed_game_time gives delta in seconds
end)
```

## Input System

The input system uses **bindings** instead of polling.

### Keyboard Bindings

```lua
-- Single press (fires once)
input_manager.bind_key("E", function()
    console.log("E pressed!")
end)

-- Held (fires every frame while held)
input_manager.bind_key_held("W", function()
    camera.dispatch_keyboard_fps(1, 0, 0)  -- Forward
end)

input_manager.bind_key_held("S", function()
    camera.dispatch_keyboard_fps(-1, 0, 0)  -- Backward
end)

input_manager.bind_key_held("A", function()
    camera.dispatch_keyboard_fps(0, 1, 0)  -- Left
end)

input_manager.bind_key_held("D", function()
    camera.dispatch_keyboard_fps(0, -1, 0)  -- Right
end)

input_manager.bind_key_held("Space", function()
    camera.dispatch_keyboard_fps(0, 0, 1)  -- Up
end)

-- Repeat (fires with delay, like typing)
input_manager.bind_key_repeat("Backspace", function()
    console.log("Backspace repeat")
end)

-- Check if key is down (for update loops)
engine.on_update(function(gt)
    if input_manager.is_key_down("ShiftLeft") then
        camera.dispatch_keyboard_fps(0, 0, -1)  -- Down
    end
end)
```

### Mouse Bindings

```lua
-- Mouse movement
input_manager.bind_mouse(function(xDelta, yDelta, posX, posY)
    local sensitivity = 0.003
    camera.dispatch_mouse_fps(yDelta * sensitivity, xDelta * sensitivity)
end)

-- Mouse clicks
input_manager.bind_mouse_click(function(button, posX, posY)
    console.log("Mouse button " .. button .. " at: " .. posX .. ", " .. posY)

    if button == "left" then
        world.remove_block()
    elseif button == "right" then
        -- Place block
    end
end)
```

### Mouse Control

```lua
-- Grab mouse (FPS mode - hidden and locked)
input_manager.grab_mouse()

-- Release mouse (visible and free)
input_manager.release_mouse()

-- Toggle mouse visibility
input_manager.toggle_mouse()

-- Check mouse state
if input_manager.is_mouse_visible() then
    console.log("Mouse is visible")
end
```

### Input Contexts

Use contexts to switch between different control schemes:

```lua
-- Default context
input_manager.bind_key("Escape", function()
    input_manager.release_mouse()
    input_manager.set_context("menu")
end)

-- Menu context
input_manager.bind_key_context("Escape", function()
    input_manager.grab_mouse()
    input_manager.set_context("default")
end, "menu")

-- Mouse for specific context
input_manager.bind_mouse_context(function(xDelta, yDelta, posX, posY)
    -- Only active in this context
end, "gameplay")

-- Get current context
local ctx = input_manager.get_context()
```

### Unbinding

```lua
input_manager.unbind_key("W")
input_manager.unbind_key_held("W")
input_manager.unbind_key_repeat("W")
input_manager.clear_bindings()  -- Remove all
```

## Camera System

### Registering Cameras

```lua
-- Register FPS camera
camera.register_fps("player")
camera.set_active("player")
```

### Camera Movement

```lua
-- FPS-style movement (relative to camera orientation)
camera.dispatch_keyboard_fps(forward, right, up)
-- forward: 1 = forward, -1 = backward
-- right: 1 = left, -1 = right
-- up: 1 = up, -1 = down

-- Example: WASD movement
input_manager.bind_key_held("W", function()
    camera.dispatch_keyboard_fps(1, 0, 0)
end)

input_manager.bind_key_held("S", function()
    camera.dispatch_keyboard_fps(-1, 0, 0)
end)
```

### Camera Rotation

```lua
-- FPS-style mouse look
camera.dispatch_mouse_fps(pitch_delta, yaw_delta)

-- Example: mouse look
input_manager.bind_mouse(function(xDelta, yDelta, posX, posY)
    local sensitivity = 0.003
    camera.dispatch_mouse_fps(yDelta * sensitivity, xDelta * sensitivity)
end)

-- Generic rotation (if needed)
camera.dispatch_mouse(yaw, pitch, roll)
```

## Window Management

```lua
-- Set window title
window.set_title("My Voxel Game")

-- Enable/disable VSync
window.set_vsync(true)

-- Set refresh rate
window.set_refresh_rate(144)

-- Get window title
local title = window.get_title()
```

## Asset Loading

### Textures

```lua
-- Load single texture
assets.load_texture("player", "textures/player.png")

-- Load texture atlas
assets.load_atlas("atlas_name", "textures/atlas.png", tile_width, tile_height, margin, spacing)

-- Example: 16x16 tiles with no margin/spacing
assets.load_atlas("blocks", "textures/blocks.png", 16, 16, 0, 0)

-- Example with margin/spacing
assets.load_atlas("ui", "textures/ui.png", 32, 32, 1, 2)
```

### Fonts

```lua
assets.load_font("main_font", "fonts/roboto.ttf")
```

### Atlas References

```lua
-- Reference atlas tiles in block definitions
"default@288"  -- Atlas "default", tile index 288
"atlas2@15"    -- Atlas "atlas2", tile index 15
```

## Notifications

```lua
-- Info notification (blue)
notifications.info("Game saved!")

-- Success notification (green)
notifications.success("Level complete!")

-- Warning notification (yellow)
notifications.warning("Low health!")

-- Error notification (red)
notifications.error("Connection lost!")

-- Test all notification types
notifications.test_all()

-- Test error notification
notifications.test_script_error()
```

## Console Logging

```lua
console.log("Info message")
console.info("Info message")
console.debug("Debug message")
console.warn("Warning message")
console.error("Error message")
console.trace("Trace message")

-- Assert
console.assert(player_health > 0, "Player health must be positive!")

-- Clear console
console.clear()
```

## Rendering

```lua
-- Toggle wireframe mode
rendering.toggle_wireframe()

-- Check wireframe state
if rendering.is_wireframe() then
    notifications.info("Wireframe mode enabled")
else
    notifications.info("Wireframe mode disabled")
end
```

## Block Registry (Voxel Plugin)

### Loading from JSON

```lua
-- Load blocks from data/blocks.json
block_registry.load_blocks_from_data("blocks.json")
```

### JSON Format

Create `data/blocks.json`:

```json
[
  {
    "name": "grass",
    "isSolid": true,
    "isBreakable": true,
    "faces": {
      "All": "default@535",
      "Top": "default@288",
      "Bottom": "default@533"
    }
  },
  {
    "name": "stone",
    "isSolid": true,
    "isBreakable": true,
    "faces": {
      "All": "default@7"
    }
  },
  {
    "name": "water",
    "isLiquid": true,
    "faces": {
      "All": "default@149"
    }
  },
  {
    "name": "glass",
    "isTransparent": true,
    "isSolid": true,
    "faces": {
      "All": "default@586"
    }
  },
  {
    "name": "flowers",
    "isBillboard": true,
    "faces": {
      "All": "default2@193"
    }
  }
]
```

### Programmatic Registration

```lua
-- Create new block
local my_block = block_registry.new_block("custom_stone")

-- Register with builder function
block_registry.register_block("glowing_block", function(block)
    block.id = 100
    block.name = "glowing_block"
    block.is_solid = true
    block.is_light_source = true
    block.emits_light = 15

    -- Set textures
    block:set_texture(block_face.Top, "blocks", 10)
    block:set_texture(block_face.Bottom, "blocks", 11)
    block:set_all_textures("blocks", 12)
end)
```

### Block Properties

```json
{
  "name": "block_name",
  "isSolid": true,           // Solid collision
  "isLiquid": false,         // Is liquid (like water)
  "isOpaque": true,          // Blocks light
  "isTransparent": false,    // See-through (like glass)
  "isBreakable": true,       // Can be broken
  "isBillboard": false,      // Renders as flat sprite (flowers)
  "isItem": false,           // Is an item
  "isLightSource": false,    // Emits light
  "emitsLight": 0,           // Light level (0-15)
  "hardness": 1.0,           // How hard to break
  "faces": {
    "All": "atlas@index",    // All faces
    "Top": "atlas@index",    // Override specific faces
    "Bottom": "atlas@index",
    "Front": "atlas@index",
    "Back": "atlas@index",
    "Left": "atlas@index",
    "Right": "atlas@index"
  }
}
```

## World Module (Voxel Plugin)

```lua
-- Remove block at raycast target
world.remove_block()

-- (More world functions may be available - check your implementation)
```

## Job System

```lua
-- Run closure on main thread
job_system.run_in_main_thread(function()
    console.log("Running on main thread")
end)

-- Schedule job on worker thread
job_system.schedule("heavy_work", function()
    -- This runs on a background thread
    local result = do_expensive_calculation()

    -- Return to main thread for UI updates
    job_system.run_in_main_thread(function()
        update_ui_with_result(result)
    end)
end, user_data)
```

## Scenes

```lua
-- Add a scene
scenes.add_scene("game", function()
    -- Scene load function
    console.log("Game scene loaded")
end)

-- Load/switch to scene
scenes.load_scene("game")
```

## Commands

Register console commands:

```lua
commands.register(
    "god",                      -- Command name
    "Enable god mode",          -- Description
    "godmode,gm",              -- Aliases (comma-separated)
    function()                  -- Execute function
        console.log("God mode enabled!")
    end
)

-- Example with parameters
commands.register("spawn", "Spawn entity", "s", function(entity_name)
    console.log("Spawning: " .. entity_name)
end)
```

## Quake Console

```lua
-- Toggle console visibility
quake_console.toggle()

-- Bind to key
input_manager.bind_key("GraveAccent", function()
    quake_console.toggle()
end)
```

## Game Objects

Create game objects from Lua:

```lua
-- Examples (check your specific implementation for parameters)
local sky = game_objects.new_sky()
local rain = game_objects.new_rain_effect()
local outline = game_objects.new_block_outline()
local fps_display = game_objects.new_fps()
local console = game_objects.new_quake_console()

-- UI objects
local button = game_objects.new_button()
local text = game_objects.new_text()
local text_edit = game_objects.new_text_edit()
local combo_box = game_objects.new_combo_box()
local list_box = game_objects.new_list_box()

-- Voxel objects
local world = game_objects.new_world()
local chunk = game_objects.new_chunk()

-- Debug objects
local perf_debugger = game_objects.new_performance_debugger()
local camera_debugger = game_objects.new_camera_debugger()
local pipeline_debugger = game_objects.new_render_pipeline_debugger()
```

## ImGui Integration

Full ImGui API is exposed for custom debug UI:

```lua
-- Basic usage
if im_gui.button("Click Me") then
    console.log("Button clicked!")
end

im_gui.text("Hello World")
im_gui.text_colored(1, 0, 0, 1, "Red text")

-- Input widgets
local new_value = im_gui.slider_float("Volume", volume, 0, 1)
local new_text = im_gui.input_text("Name", player_name, 256)
local checked = im_gui.checkbox("Enable", is_enabled)

-- Layout
im_gui.same_line()
im_gui.separator()
im_gui.spacing()
im_gui.new_line()

-- Trees and headers
if im_gui.tree_node("Settings") then
    im_gui.text("Nested content")
    im_gui.tree_pop()
end

if im_gui.collapsing_header("Advanced") then
    im_gui.text("Collapsible content")
end
```

## Engine Lifecycle

```lua
-- Called once on initialization
function on_initialize()
    window.set_title("My Game")
    -- Setup code here
end

-- Called every frame
engine.on_update(function(game_time)
    -- game_time.elapsed_game_time - delta time in seconds
    -- game_time.total_game_time - total elapsed time

    -- Update game logic here
end)
```

## Complete Example: FPS Voxel Game

```lua
-- scripts/init.lua

local blocks = require("blocks")

-- Initialization
function on_initialize()
    window.set_title("Squid Voxel")
    window.set_vsync(true)
    window.set_refresh_rate(70)

    -- Setup camera
    camera.register_fps("main")
    camera.set_active("main")

    -- Load assets
    assets.load_atlas("default", "textures/blocks.png", 16, 16, 0, 0)
    blocks.load_tiles()
end

-- Mouse toggle
input_manager.bind_key("F5", function()
    input_manager.toggle_mouse()
end)

-- Wireframe toggle
input_manager.bind_key("F3", function()
    rendering.toggle_wireframe()
    local state = rendering.is_wireframe() and "enabled" or "disabled"
    notifications.info("Wireframe mode " .. state)
end)

-- Console toggle
input_manager.bind_key("GraveAccent", function()
    quake_console.toggle()
end)

-- Mouse look
input_manager.bind_mouse(function(xDelta, yDelta, posX, posY)
    local sensitivity = 0.003
    camera.dispatch_mouse_fps(yDelta * sensitivity, xDelta * sensitivity)
end)

-- Mouse clicks
input_manager.bind_mouse_click(function(button, posX, posY)
    if button == "left" then
        world.remove_block()
    elseif button == "right" then
        -- Place block (implement your logic)
    end
end)

-- Movement
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

-- Shift for down (modifier keys need checking in update)
engine.on_update(function(gt)
    if input_manager.is_key_down("ShiftLeft") or input_manager.is_key_down("ShiftRight") then
        camera.dispatch_keyboard_fps(0, 0, -1)
    end
end)

-- Release mouse with Escape
input_manager.bind_key("Escape", function()
    input_manager.release_mouse()
end)

-- Start with mouse grabbed
input_manager.grab_mouse()
```

## Module Pattern

Use modules for organization:

```lua
-- scripts/blocks/init.lua
M = {}

function M.load_tiles()
    block_registry.load_blocks_from_data("blocks.json")
end

return M
```

```lua
-- scripts/init.lua
local blocks = require("blocks")
blocks.load_tiles()
```

## Auto-generated Definitions

The engine generates `definitions.lua` with LSP annotations. This file is auto-updated and provides autocomplete in your IDE.

Example of what's generated:

```lua
---@class console
console = {}

---@param message string The message text
function console.log(message) end

---@class input_manager
input_manager = {}

---@param key_binding string The keybinding text
---@param callback function The callback of type function
function input_manager.bind_key(key_binding, callback) end
```

Use this with Lua Language Server for full autocomplete!

## Best Practices

1. **Use local variables** - Faster than globals
2. **Cache module references** - Store in locals if used frequently
3. **Bind input once** - Do bindings in initialization, not every frame
4. **Use engine.on_update for polling** - Not for bindings
5. **Use job_system for heavy work** - Keep main thread responsive
6. **Load assets on initialization** - Not during gameplay
7. **Use modules for organization** - Keep scripts manageable

## Debugging

```lua
-- Detailed logging
console.log("Player health: " .. player_health)
console.debug("Debug info: " .. debug_value)

-- Assertions
console.assert(player ~= nil, "Player must exist!")

-- Test notifications
notifications.test_all()

-- Use ImGui for custom debug UI
im_gui.text("FPS: " .. tostring(fps))
if im_gui.button("Reset") then
    reset_game()
end
```

## Next Steps

- [Getting Started](getting-started.md) - Integrate Lua in your first scene
- [Plugin Development](plugin-development.md) - Expose new APIs to Lua
- [Voxel Tutorial](tutorials/voxel-world.md) - Complete voxel game with Lua
- Check `definitions.lua` for complete API reference with LSP annotations

Your engine's Lua API is fully documented in the auto-generated `definitions.lua` file!