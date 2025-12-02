# Lua Scripting Guide

Lilly.Engine uses Lua (through MoonSharp) for game logic, allowing you to write and modify behavior without recompiling. This guide covers everything from basic scripts to advanced integration.

## Why Lua?

**Fast Iteration** - Change game logic without rebuilding
**Modding** - Players can create mods with scripts
**Prototyping** - Test ideas quickly
**Hot Reload** - Scripts reload when files change

## Getting Started

### Your First Script

Create `scripts/hello.lua`:

```lua
function greet(name)
    console.log("Hello, " .. name .. "!")
    return "Greeted " .. name
end
```

Load and execute from C#:

```csharp
public class MyScene : BaseScene
{
    private readonly IScriptEngineService _scriptEngine;

    public MyScene(IScriptEngineService scriptEngine)
    {
        _scriptEngine = scriptEngine;
    }

    public override void Initialize()
    {
        var script = _scriptEngine.LoadScript("scripts/hello.lua");
        var result = script.Call("greet", "World");

        // Output: "Hello, World!"
        // result contains: "Greeted World"
    }
}
```

### Script Structure

Lua scripts can define:

- **Functions** - Called from C#
- **Variables** - Shared state
- **Tables** - Data structures
- **Event handlers** - React to game events

```lua
-- Global variables
local player_health = 100
local player_speed = 5.0

-- Initialization
function on_init()
    console.log("Script initialized")
end

-- Update loop
function on_update(delta_time)
    -- Game logic here
end

-- Event handler
function on_player_hit(damage)
    player_health = player_health - damage

    if player_health <= 0 then
        on_player_died()
    end
end

-- Helper functions
local function respawn_player()
    player_health = 100
end
```

## Available Modules

The engine exposes these Lua modules:

### console

Logging and output:

```lua
console.log("Info message")
console.warn("Warning message")
console.error("Error message")
```

### engine

Core engine access:

```lua
-- Get delta time
local dt = engine.get_delta_time()

-- Get current FPS
local fps = engine.get_fps()

-- Exit the game
engine.exit()

-- Input access
local input = engine.input
if input:is_key_down("W") then
    -- Move forward
end
```

### input

Input handling:

```lua
-- Keyboard
if input:is_key_down("Space") then
    jump()
end

if input:is_key_pressed("E") then  -- Single press
    interact()
end

if input:is_key_released("Shift") then
    stop_sprinting()
end

-- Mouse
local mouse_x, mouse_y = input:get_mouse_position()
local scroll = input:get_mouse_wheel_delta()

if input:is_mouse_button_down("Left") then
    fire()
end

-- Gamepad
if input:is_gamepad_button_down("A") then
    jump()
end

local left_x, left_y = input:get_gamepad_left_stick()
local right_x, right_y = input:get_gamepad_right_stick()
```

### assets

Asset management:

```lua
-- Load texture
local texture = assets.load_texture("path/to/texture.png")

-- Load font
local font = assets.load_font("path/to/font.ttf", 24)

-- Load sound
local sound = assets.load_sound("path/to/sound.wav")

-- Unload asset
assets.unload("path/to/texture.png")
```

### camera

Camera control:

```lua
-- Get camera position
local x, y, z = camera.get_position()

-- Set camera position
camera.set_position(0, 10, 20)

-- Get camera rotation
local pitch, yaw, roll = camera.get_rotation()

-- Set camera rotation
camera.set_rotation(0, 90, 0)

-- Move camera
camera.move(dx, dy, dz)

-- Rotate camera
camera.rotate(d_pitch, d_yaw, d_roll)
```

### scenes

Scene management:

```lua
-- Switch to another scene
scenes.activate("GameOver")

-- Switch with transition
scenes.activate_with_transition("MainMenu", "Fade", 1.0)

-- Get active scene name
local current = scenes.get_active()
```

### notifications

In-game notifications:

```lua
notifications.show("Level Complete!", "Success")
notifications.show("Low health", "Warning")
notifications.show("Connection lost", "Error")
notifications.show("Checkpoint saved", "Info")
```

### jobs

Job system for multi-threading:

```lua
-- Enqueue background work
jobs.enqueue(function()
    -- This runs on a worker thread
    local result = expensive_calculation()

    -- Return to main thread
    jobs.enqueue_main_thread(function()
        handle_result(result)
    end)
end)

-- Enqueue with priority
jobs.enqueue_high_priority(function()
    -- Critical work
end)
```

### commands

Command system:

```lua
-- Register a command
commands.register("heal", function(amount)
    player_health = math.min(100, player_health + amount)
    notifications.show("Healed " .. amount .. " HP", "Success")
end)

-- Execute a command
commands.execute("heal", 25)

-- Check if command exists
if commands.exists("god_mode") then
    commands.execute("god_mode")
end
```

### world (Voxel Plugin)

Voxel world control:

```lua
-- Get block at position
local block = world.get_block(x, y, z)

-- Set block
world.set_block(x, y, z, block_id)

-- Get chunk at position
local chunk = world.get_chunk(chunk_x, chunk_z)

-- Regenerate chunk
world.regenerate_chunk(chunk_x, chunk_z)

-- Get world height at position
local height = world.get_height(x, z)
```

### blocks (Voxel Plugin)

Block registry:

```lua
-- Register custom block
blocks.register({
    id = 100,
    name = "custom_stone",
    is_solid = true,
    is_transparent = false,
    texture_top = "blocks/stone_top.png",
    texture_sides = "blocks/stone_side.png",
    texture_bottom = "blocks/stone_bottom.png"
})

-- Get block info
local info = blocks.get(100)
console.log("Block name: " .. info.name)
```

### generation (Voxel Plugin)

Terrain generation:

```lua
-- Add custom generation step
generation.add_step("custom_structures", function(chunk)
    -- Generate structures in this chunk
    for x = 0, 15 do
        for z = 0, 15 do
            if should_place_tree(x, z) then
                place_tree(chunk, x, z)
            end
        end
    end
end)

-- Remove generation step
generation.remove_step("caves")

-- Get generation settings
local settings = generation.get_settings()
console.log("Seed: " .. settings.seed)
```

## Passing Data Between C# and Lua

### C# to Lua

Pass parameters to Lua functions:

```csharp
// Simple types
script.Call("update", deltaTime);
script.Call("set_health", 100);
script.Call("set_name", "Player");

// Multiple parameters
script.Call("move", x, y, z);

// Tables (as Dictionary)
var config = new Dictionary<string, object>
{
    ["speed"] = 5.0,
    ["jumpHeight"] = 2.0,
    ["canFly"] = false
};
script.Call("configure", config);
```

### Lua to C#

Return values from Lua:

```csharp
// Single return value
var result = script.Call("calculate");
var number = result[0] as double?;
var text = result[0] as string;

// Multiple return values
var result = script.Call("get_position");
var x = result[0] as double? ?? 0;
var y = result[1] as double? ?? 0;
var z = result[2] as double? ?? 0;
```

```lua
function get_position()
    return player.x, player.y, player.z
end

function calculate()
    return 42
end
```

## Practical Examples

### Player Controller

```lua
-- player_controller.lua

local player = {
    x = 0,
    y = 0,
    z = 0,
    speed = 5.0,
    jump_force = 10.0,
    is_grounded = true
}

function on_init()
    console.log("Player controller initialized")
end

function on_update(delta_time)
    handle_movement(delta_time)
    handle_jump()
    apply_gravity(delta_time)

    return player.x, player.y, player.z
end

function handle_movement(dt)
    local input = engine.input
    local move_x = 0
    local move_z = 0

    if input:is_key_down("W") then move_z = move_z - 1 end
    if input:is_key_down("S") then move_z = move_z + 1 end
    if input:is_key_down("A") then move_x = move_x - 1 end
    if input:is_key_down("D") then move_x = move_x + 1 end

    -- Normalize diagonal movement
    if move_x ~= 0 or move_z ~= 0 then
        local length = math.sqrt(move_x * move_x + move_z * move_z)
        move_x = move_x / length
        move_z = move_z / length
    end

    player.x = player.x + move_x * player.speed * dt
    player.z = player.z + move_z * player.speed * dt
end

function handle_jump()
    if player.is_grounded and engine.input:is_key_pressed("Space") then
        player.y = player.y + player.jump_force
        player.is_grounded = false
    end
end

function apply_gravity(dt)
    if not player.is_grounded then
        player.y = player.y - 9.8 * dt

        if player.y <= 0 then
            player.y = 0
            player.is_grounded = true
        end
    end
end
```

Use in C#:

```csharp
private readonly IScriptEngineService _scriptEngine;
private LuaScript? _playerScript;
private Vector3D<float> _playerPosition;

public override void Initialize()
{
    _playerScript = _scriptEngine.LoadScript("scripts/player_controller.lua");
    _playerScript.Call("on_init");
}

public override void Update(float deltaTime)
{
    if (_playerScript != null)
    {
        var result = _playerScript.Call("on_update", deltaTime);

        _playerPosition.X = (float)(result[0] as double? ?? 0);
        _playerPosition.Y = (float)(result[1] as double? ?? 0);
        _playerPosition.Z = (float)(result[2] as double? ?? 0);
    }
}
```

### Enemy AI

```lua
-- enemy_ai.lua

local enemies = {}

function spawn_enemy(id, x, y, z)
    enemies[id] = {
        x = x,
        y = y,
        z = z,
        health = 100,
        speed = 3.0,
        state = "idle",
        target = nil
    }
end

function update_enemies(player_x, player_y, player_z, delta_time)
    for id, enemy in pairs(enemies) do
        update_enemy(enemy, player_x, player_y, player_z, delta_time)
    end
end

function update_enemy(enemy, px, py, pz, dt)
    local dx = px - enemy.x
    local dy = py - enemy.y
    local dz = pz - enemy.z
    local distance = math.sqrt(dx*dx + dy*dy + dz*dz)

    if distance < 10 then
        -- Player is close, chase
        enemy.state = "chase"

        local move_x = (dx / distance) * enemy.speed * dt
        local move_z = (dz / distance) * enemy.speed * dt

        enemy.x = enemy.x + move_x
        enemy.z = enemy.z + move_z

        if distance < 2 then
            attack_player()
        end
    else
        enemy.state = "idle"
    end
end

function attack_player()
    notifications.show("Enemy attacked!", "Warning")
end

function remove_enemy(id)
    enemies[id] = nil
end

function get_enemy_positions()
    local positions = {}
    for id, enemy in pairs(enemies) do
        table.insert(positions, {
            id = id,
            x = enemy.x,
            y = enemy.y,
            z = enemy.z,
            state = enemy.state
        })
    end
    return positions
end
```

### Terrain Generator

```lua
-- custom_terrain.lua

local noise = require("noise")

generation.add_step("custom_mountains", function(chunk)
    local chunk_x = chunk.x * 16
    local chunk_z = chunk.z * 16

    for x = 0, 15 do
        for z = 0, 15 do
            local world_x = chunk_x + x
            local world_z = chunk_z + z

            -- Generate height using noise
            local height = noise.get(world_x * 0.01, world_z * 0.01)
            height = height * 50 + 64  -- Scale and offset

            -- Fill from bottom to height
            for y = 0, math.floor(height) do
                if y < height - 3 then
                    world.set_block(world_x, y, world_z, 1)  -- Stone
                elseif y < height then
                    world.set_block(world_x, y, world_z, 3)  -- Dirt
                else
                    world.set_block(world_x, y, world_z, 2)  -- Grass
                end
            end
        end
    end
end)
```

## Hot Reload

Scripts automatically reload when files change:

1. Start the game
2. Edit a script file
3. Save the file
4. Script reloads automatically

Callbacks are preserved - your `on_update` keeps running with the new code.

To manually reload:

```csharp
_scriptEngine.ReloadScript("scripts/player.lua");
```

## Error Handling

Lua errors don't crash the engine. They're logged and the script continues:

```lua
function risky_operation()
    local result, err = pcall(function()
        -- Code that might error
        local value = undefined_variable + 10
    end)

    if not result then
        console.error("Operation failed: " .. err)
        return nil
    end

    return result
end
```

From C#:

```csharp
try
{
    var result = script.Call("risky_function");
}
catch (ScriptRuntimeException ex)
{
    _logger.Error(ex, "Script error in {Script}", scriptPath);
}
```

## Performance Tips

### Cache Expensive Lookups

```lua
-- Bad: Looks up every frame
function on_update(dt)
    if engine.input:is_key_down("W") then
        move_forward(dt)
    end
end

-- Good: Cache the input module
local input = engine.input

function on_update(dt)
    if input:is_key_down("W") then
        move_forward(dt)
    end
end
```

### Use Local Variables

```lua
-- Bad: Global variables are slower
speed = 5.0

function move(dt)
    player.x = player.x + speed * dt
end

-- Good: Local variables are faster
local speed = 5.0

function move(dt)
    player.x = player.x + speed * dt
end
```

### Avoid Creating Tables in Loops

```lua
-- Bad: Creates new table every frame
function on_update(dt)
    for i = 1, 100 do
        local pos = {x = 0, y = 0, z = 0}
        -- Use pos
    end
end

-- Good: Reuse table
local temp_pos = {x = 0, y = 0, z = 0}

function on_update(dt)
    for i = 1, 100 do
        temp_pos.x = 0
        temp_pos.y = 0
        temp_pos.z = 0
        -- Use temp_pos
    end
end
```

### Use Jobs for Heavy Work

```lua
function generate_terrain()
    jobs.enqueue(function()
        -- Heavy terrain generation on worker thread
        local terrain_data = generate_heightmap()

        jobs.enqueue_main_thread(function()
            -- Apply to world on main thread
            apply_terrain(terrain_data)
        end)
    end)
end
```

## Debugging Lua Scripts

### Print Debugging

```lua
console.log("Player position: " .. player.x .. ", " .. player.y)
console.log("Health: " .. tostring(player.health))

-- Table inspection
local function print_table(t, indent)
    indent = indent or 0
    for k, v in pairs(t) do
        console.log(string.rep("  ", indent) .. k .. " = " .. tostring(v))
        if type(v) == "table" then
            print_table(v, indent + 1)
        end
    end
end

print_table(player)
```

### Conditional Logging

```lua
local DEBUG = true

local function debug_log(msg)
    if DEBUG then
        console.log("[DEBUG] " .. msg)
    end
end

debug_log("Enemy spawned at " .. x .. ", " .. z)
```

### Error Boundaries

```lua
function safe_call(func, ...)
    local success, result = pcall(func, ...)
    if not success then
        console.error("Error: " .. tostring(result))
        return nil
    end
    return result
end

-- Use it
safe_call(risky_function, arg1, arg2)
```

## Lua Meta Files

The engine generates `.meta.lua` files with type hints and documentation:

```lua
-- player.meta.lua
---@class PlayerModule
---@field health number
---@field speed number
local PlayerModule = {}

---Moves the player
---@param dx number Delta X
---@param dy number Delta Y
function PlayerModule.move(dx, dy) end
```

These files help IDE autocomplete and type checking (with Lua Language Server).

## Advanced: Custom Lua Modules

Create your own Lua modules in C#:

```csharp
public class CustomLuaModule : ILuaModule
{
    public string ModuleName => "custom";

    public void RegisterModule(Script script)
    {
        var table = new Table(script);

        // Register functions
        table["greet"] = (Func<string, string>)Greet;
        table["calculate"] = (Func<double, double, double>)Calculate;

        script.Globals[ModuleName] = table;
    }

    private string Greet(string name)
    {
        return $"Hello, {name}!";
    }

    private double Calculate(double a, double b)
    {
        return a * b;
    }
}
```

Register it:

```csharp
_scriptEngine.RegisterModule(new CustomLuaModule());
```

Use in Lua:

```lua
local result = custom.greet("World")
local product = custom.calculate(5, 10)
```

## Best Practices

**Keep Scripts Small** - One script, one responsibility
**Use Comments** - Document functions and complex logic
**Handle Errors** - Use `pcall` for risky operations
**Cache Lookups** - Store module references, don't look up every frame
**Prefer Local** - Local variables are faster than globals
**Use Jobs** - Move heavy work to background threads
**Test Incrementally** - Hot reload makes testing quick

## Next Steps

- [Getting Started](getting-started.md) - Learn to integrate scripts in scenes
- [Plugin Development](plugin-development.md) - Expose your plugin APIs to Lua
- [Voxel Tutorial](tutorials/voxel-world.md) - Complete example with Lua scripts
- [UI Tutorial](tutorials/custom-ui.md) - Building interactive interfaces

Happy scripting!