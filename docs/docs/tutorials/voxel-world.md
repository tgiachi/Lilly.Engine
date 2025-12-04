# Building a Voxel World

This tutorial walks through creating a Minecraft-style voxel world with custom blocks, terrain generation, and player interaction.

## What You'll Build

By the end of this tutorial, you'll have:

- A procedurally generated voxel world
- Custom block types with textures
- Player movement and camera control
- Block placement and destruction
- Custom terrain generation with Lua

## Prerequisites

Make sure the Voxel Plugin is enabled in your `Program.cs`:

```csharp
bootstrap.OnConfiguring += _ =>
{
    container.RegisterPlugin(typeof(LillyVoxelPlugin).Assembly);
};
```

## Step 1: Basic World Setup

Create a new scene for the voxel world:

```csharp
using Lilly.Engine.Core.Interfaces;
using Lilly.Engine.Scenes;
using Lilly.Engine.Cameras;
using Lilly.Voxel.Plugin.GameObjects;
using Lilly.Voxel.Plugin.Data;
using Silk.NET.Maths;

namespace Lilly.Engine.Game.Scenes;

public class VoxelWorldScene : BaseScene
{
    private readonly IAssetManager _assetManager;
    private readonly IInputManagerService _input;

    private WorldGameObject? _world;
    private FPSCamera? _camera;

    public VoxelWorldScene(
        IAssetManager assetManager,
        IInputManagerService input)
    {
        _assetManager = assetManager;
        _input = input;
    }

    public override void Initialize()
    {
        // Create FPS camera
        _camera = new FPSCamera(
            position: new Vector3D<float>(0, 80, 0),
            aspectRatio: 16f / 9f
        );
        Camera = _camera;

        // Create world with default settings
        var settings = new WorldGenerationSettings
        {
            Seed = 12345,
            RenderDistance = 8
        };

        _world = new WorldGameObject(settings);
        AddGameObject(_world);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        HandleCameraMovement(deltaTime);
    }

    private void HandleCameraMovement(float deltaTime)
    {
        if (_camera == null) return;

        float speed = 10f;
        if (_input.IsKeyDown(Silk.NET.Input.Key.ShiftLeft))
            speed = 50f;  // Sprint

        var movement = Vector3D<float>.Zero;

        if (_input.IsKeyDown(Silk.NET.Input.Key.W))
            movement += _camera.Forward;
        if (_input.IsKeyDown(Silk.NET.Input.Key.S))
            movement -= _camera.Forward;
        if (_input.IsKeyDown(Silk.NET.Input.Key.A))
            movement -= _camera.Right;
        if (_input.IsKeyDown(Silk.NET.Input.Key.D))
            movement += _camera.Right;
        if (_input.IsKeyDown(Silk.NET.Input.Key.Space))
            movement.Y += 1;
        if (_input.IsKeyDown(Silk.NET.Input.Key.ControlLeft))
            movement.Y -= 1;

        if (movement.LengthSquared > 0)
        {
            movement = Vector3D.Normalize(movement);
            _camera.Position += movement * speed * deltaTime;
        }

        // Mouse look
        var mouseDelta = _input.GetMouseDelta();
        if (mouseDelta.LengthSquared > 0)
        {
            _camera.Rotate(mouseDelta.Y * 0.1f, mouseDelta.X * 0.1f);
        }
    }
}
```

Register and activate the scene:

```csharp
sceneManager.RegisterScene<VoxelWorldScene>("VoxelWorld");
sceneManager.ActivateScene("VoxelWorld");
```

Run the game. You should see a procedurally generated world with grass, stone, and terrain features.

## Step 2: Custom Block Types

Custom blocks are defined in `data/blocks.json`:

```json
[
  {
    "name": "grass",
    "isSolid": true,
    "isBreakable": true,
    "faces": {
      "All": "blocks@535",
      "Top": "blocks@288",
      "Bottom": "blocks@533"
    }
  },
  {
    "name": "stone",
    "isSolid": true,
    "isBreakable": true,
    "faces": {
      "All": "blocks@7"
    }
  },
  {
    "name": "glass",
    "isTransparent": true,
    "isSolid": true,
    "faces": {
      "All": "blocks@586"
    }
  },
  {
    "name": "glowing_crystal",
    "isSolid": true,
    "isLightSource": true,
    "emitsLight": 15,
    "faces": {
      "All": "blocks@420"
    }
  },
  {
    "name": "flowers",
    "isBillboard": true,
    "faces": {
      "All": "blocks@193"
    }
  }
]
```

Load blocks in your Lua init script:

```lua
-- scripts/init.lua
local blocks = require("blocks")

function on_initialize()
    -- Load texture atlas
    assets.load_atlas("blocks", "textures/blocks.png", 16, 16, 0, 0)

    -- Load block definitions from JSON
    blocks.load_tiles()

    console.log("Blocks loaded!")
end
```

```lua
-- scripts/blocks/init.lua
M = {}

function M.load_tiles()
    block_registry.load_blocks_from_data("blocks.json")
end

return M
```

## Step 3: Block Placement and Destruction

Add interaction to place and destroy blocks:

```csharp
private int _selectedBlockType = 1;  // Default: stone
private BlockOutlineGameObject? _blockOutline;

public override void Initialize()
{
    // ... existing initialization ...

    // Add block outline for targeted block
    _blockOutline = new BlockOutlineGameObject();
    AddGameObject(_blockOutline);
}

public override void Update(float deltaTime)
{
    base.Update(deltaTime);
    HandleCameraMovement(deltaTime);
    HandleBlockInteraction();
    HandleBlockSelection();
}

private void HandleBlockInteraction()
{
    if (_world == null || _camera == null) return;

    // Raycast from camera to find targeted block
    var ray = _camera.GetRay();
    var hit = _world.Raycast(ray, maxDistance: 10f);

    if (hit.HasValue)
    {
        // Update block outline
        if (_blockOutline != null)
        {
            _blockOutline.Position = hit.Value.BlockPosition;
            _blockOutline.Visible = true;
        }

        // Left click: destroy block
        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            _world.SetBlock(
                hit.Value.BlockPosition.X,
                hit.Value.BlockPosition.Y,
                hit.Value.BlockPosition.Z,
                0  // 0 = air (remove block)
            );
        }

        // Right click: place block
        if (_input.IsMouseButtonPressed(MouseButton.Right))
        {
            var placePos = hit.Value.BlockPosition + hit.Value.Normal;

            _world.SetBlock(
                placePos.X,
                placePos.Y,
                placePos.Z,
                _selectedBlockType
            );
        }
    }
    else
    {
        // No block in range
        if (_blockOutline != null)
            _blockOutline.Visible = false;
    }
}

private void HandleBlockSelection()
{
    // Number keys to select block type
    if (_input.IsKeyPressed(Key.Number1)) _selectedBlockType = 1;   // Stone
    if (_input.IsKeyPressed(Key.Number2)) _selectedBlockType = 2;   // Grass
    if (_input.IsKeyPressed(Key.Number3)) _selectedBlockType = 3;   // Dirt
    if (_input.IsKeyPressed(Key.Number4)) _selectedBlockType = 100; // Crystal
    if (_input.IsKeyPressed(Key.Number5)) _selectedBlockType = 101; // Glass
    if (_input.IsKeyPressed(Key.Number6)) _selectedBlockType = 102; // Wood
}
```

## Step 4: Input Bindings with Lua

Set up camera controls using Lua input bindings:

```lua
-- scripts/init.lua
local blocks = require("blocks")

function on_initialize()
    window.set_title("Voxel World")
    window.set_vsync(true)

    -- Setup camera
    camera.register_fps("main")
    camera.set_active("main")

    -- Load assets and blocks
    assets.load_atlas("blocks", "textures/blocks.png", 16, 16, 0, 0)
    blocks.load_tiles()

    console.log("Voxel world initialized!")
end

-- Mouse controls
input_manager.bind_mouse(function(xDelta, yDelta, posX, posY)
    local sensitivity = 0.003
    camera.dispatch_mouse_fps(yDelta * sensitivity, xDelta * sensitivity)
end)

-- Click to remove blocks
input_manager.bind_mouse_click(function(button, posX, posY)
    if button == "left" then
        world.remove_block()
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

-- Shift for down
engine.on_update(function(gt)
    if input_manager.is_key_down("ShiftLeft") or input_manager.is_key_down("ShiftRight") then
        camera.dispatch_keyboard_fps(0, 0, -1)
    end
end)

-- Toggle mouse
input_manager.bind_key("F5", function()
    input_manager.toggle_mouse()
end)

-- Wireframe toggle
input_manager.bind_key("F3", function()
    rendering.toggle_wireframe()
    local state = rendering.is_wireframe() and "enabled" or "disabled"
    notifications.info("Wireframe mode " .. state)
end)

-- Start with mouse grabbed
input_manager.grab_mouse()
```

## Step 5: Add UI for Block Selection

Display the currently selected block:

```csharp
private TextGameObject? _blockSelectionText;

public override void Initialize()
{
    // ... existing initialization ...

    // Add UI for block selection
    var font = _assetManager.LoadFont(
        "Lilly.Engine.Assets.Fonts.Roboto-Regular.ttf", 24);

    _blockSelectionText = new TextGameObject(font)
    {
        Position = new Vector2D<float>(10, 10),
        Color = Color.White,
        Text = "Selected: Stone"
    };
    AddGameObject(_blockSelectionText);
}

private void HandleBlockSelection()
{
    string blockName = "Unknown";

    // Number keys to select block type
    if (_input.IsKeyPressed(Key.Number1))
    {
        _selectedBlockType = 1;
        blockName = "Stone";
    }
    if (_input.IsKeyPressed(Key.Number2))
    {
        _selectedBlockType = 2;
        blockName = "Grass";
    }
    if (_input.IsKeyPressed(Key.Number3))
    {
        _selectedBlockType = 3;
        blockName = "Dirt";
    }
    if (_input.IsKeyPressed(Key.Number4))
    {
        _selectedBlockType = 100;
        blockName = "Crystal";
    }
    if (_input.IsKeyPressed(Key.Number5))
    {
        _selectedBlockType = 101;
        blockName = "Glass";
    }
    if (_input.IsKeyPressed(Key.Number6))
    {
        _selectedBlockType = 102;
        blockName = "Wood";
    }

    if (_blockSelectionText != null)
    {
        _blockSelectionText.Text = $"Selected: {blockName}";
    }
}
```

## Step 6: Advanced Features

### Day/Night Cycle

Add a dynamic sky:

```csharp
private SkyGameObject? _sky;
private float _timeOfDay = 0.5f;  // 0 = midnight, 0.5 = noon, 1 = midnight

public override void Initialize()
{
    // ... existing initialization ...

    _sky = new SkyGameObject();
    AddGameObject(_sky);
}

public override void Update(float deltaTime)
{
    base.Update(deltaTime);

    // Update time of day
    _timeOfDay += deltaTime * 0.01f;  // Slow cycle
    if (_timeOfDay > 1f) _timeOfDay = 0f;

    if (_sky != null)
    {
        _sky.TimeOfDay = _timeOfDay;
    }

    // ... rest of update ...
}
```

### Weather Effects

Add rain or snow:

```csharp
private RainEffectGameObject? _rain;

public override void Initialize()
{
    // ... existing initialization ...

    _rain = new RainEffectGameObject
    {
        Intensity = 0.5f,
        Position = _camera!.Position
    };
    AddGameObject(_rain);
}

public override void Update(float deltaTime)
{
    base.Update(deltaTime);

    // Rain follows camera
    if (_rain != null && _camera != null)
    {
        _rain.Position = _camera.Position;
    }

    // Toggle rain with R key
    if (_input.IsKeyPressed(Key.R))
    {
        _rain!.Visible = !_rain.Visible;
    }

    // ... rest of update ...
}
```

### Chunk Debugging

Visualize chunk boundaries:

```csharp
private ChunkDebuggerViewerGameObject? _chunkDebugger;

public override void Initialize()
{
    // ... existing initialization ...

    _chunkDebugger = new ChunkDebuggerViewerGameObject(_world!);
    AddGameObject(_chunkDebugger);
}

public override void Update(float deltaTime)
{
    base.Update(deltaTime);

    // Toggle chunk debugger with F5
    if (_input.IsKeyPressed(Key.F5))
    {
        _chunkDebugger!.Visible = !_chunkDebugger.Visible;
    }

    // ... rest of update ...
}
```

## Complete Scene Code

Here's the full scene with all features:

```csharp
public class VoxelWorldScene : BaseScene
{
    private readonly IAssetManager _assetManager;
    private readonly IInputManagerService _input;
    private readonly IScriptEngineService _scriptEngine;

    private WorldGameObject? _world;
    private FPSCamera? _camera;
    private BlockOutlineGameObject? _blockOutline;
    private TextGameObject? _blockSelectionText;
    private SkyGameObject? _sky;
    private RainEffectGameObject? _rain;

    private int _selectedBlockType = 1;
    private float _timeOfDay = 0.5f;

    public VoxelWorldScene(
        IAssetManager assetManager,
        IInputManagerService input,
        IScriptEngineService scriptEngine)
    {
        _assetManager = assetManager;
        _input = input;
        _scriptEngine = scriptEngine;
    }

    public override void Initialize()
    {
        // Load scripts
        var blockScript = _scriptEngine.LoadScript("scripts/custom_blocks.lua");
        blockScript.Call("register_custom_blocks");

        var terrainScript = _scriptEngine.LoadScript("scripts/terrain_generation.lua");
        terrainScript.Call("generate_custom_terrain");

        // Camera
        _camera = new FPSCamera(
            position: new Vector3D<float>(0, 80, 0),
            aspectRatio: 16f / 9f
        );
        Camera = _camera;

        // World
        var settings = new WorldGenerationSettings
        {
            Seed = 12345,
            RenderDistance = 8
        };
        _world = new WorldGameObject(settings);
        AddGameObject(_world);

        // Sky
        _sky = new SkyGameObject();
        AddGameObject(_sky);

        // Block outline
        _blockOutline = new BlockOutlineGameObject();
        AddGameObject(_blockOutline);

        // Rain
        _rain = new RainEffectGameObject { Intensity = 0.5f, Visible = false };
        AddGameObject(_rain);

        // UI
        var font = _assetManager.LoadFont(
            "Lilly.Engine.Assets.Fonts.Roboto-Regular.ttf", 24);

        _blockSelectionText = new TextGameObject(font)
        {
            Position = new Vector2D<float>(10, 10),
            Color = Color.White,
            Text = "Selected: Stone"
        };
        AddGameObject(_blockSelectionText);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        HandleCameraMovement(deltaTime);
        HandleBlockInteraction();
        HandleBlockSelection();
        UpdateDayNightCycle(deltaTime);
        HandleWeather();
    }

    private void HandleCameraMovement(float deltaTime)
    {
        if (_camera == null) return;

        float speed = 10f;
        if (_input.IsKeyDown(Key.ShiftLeft)) speed = 50f;

        var movement = Vector3D<float>.Zero;

        if (_input.IsKeyDown(Key.W)) movement += _camera.Forward;
        if (_input.IsKeyDown(Key.S)) movement -= _camera.Forward;
        if (_input.IsKeyDown(Key.A)) movement -= _camera.Right;
        if (_input.IsKeyDown(Key.D)) movement += _camera.Right;
        if (_input.IsKeyDown(Key.Space)) movement.Y += 1;
        if (_input.IsKeyDown(Key.ControlLeft)) movement.Y -= 1;

        if (movement.LengthSquared > 0)
        {
            movement = Vector3D.Normalize(movement);
            _camera.Position += movement * speed * deltaTime;
        }

        var mouseDelta = _input.GetMouseDelta();
        if (mouseDelta.LengthSquared > 0)
        {
            _camera.Rotate(mouseDelta.Y * 0.1f, mouseDelta.X * 0.1f);
        }
    }

    private void HandleBlockInteraction()
    {
        if (_world == null || _camera == null) return;

        var ray = _camera.GetRay();
        var hit = _world.Raycast(ray, maxDistance: 10f);

        if (hit.HasValue)
        {
            if (_blockOutline != null)
            {
                _blockOutline.Position = hit.Value.BlockPosition;
                _blockOutline.Visible = true;
            }

            if (_input.IsMouseButtonPressed(MouseButton.Left))
            {
                _world.SetBlock(
                    hit.Value.BlockPosition.X,
                    hit.Value.BlockPosition.Y,
                    hit.Value.BlockPosition.Z,
                    0);
            }

            if (_input.IsMouseButtonPressed(MouseButton.Right))
            {
                var placePos = hit.Value.BlockPosition + hit.Value.Normal;
                _world.SetBlock(placePos.X, placePos.Y, placePos.Z, _selectedBlockType);
            }
        }
        else
        {
            if (_blockOutline != null) _blockOutline.Visible = false;
        }
    }

    private void HandleBlockSelection()
    {
        string? blockName = null;

        if (_input.IsKeyPressed(Key.Number1)) { _selectedBlockType = 1; blockName = "Stone"; }
        if (_input.IsKeyPressed(Key.Number2)) { _selectedBlockType = 2; blockName = "Grass"; }
        if (_input.IsKeyPressed(Key.Number3)) { _selectedBlockType = 3; blockName = "Dirt"; }
        if (_input.IsKeyPressed(Key.Number4)) { _selectedBlockType = 100; blockName = "Crystal"; }
        if (_input.IsKeyPressed(Key.Number5)) { _selectedBlockType = 101; blockName = "Glass"; }
        if (_input.IsKeyPressed(Key.Number6)) { _selectedBlockType = 102; blockName = "Wood"; }

        if (blockName != null && _blockSelectionText != null)
        {
            _blockSelectionText.Text = $"Selected: {blockName}";
        }
    }

    private void UpdateDayNightCycle(float deltaTime)
    {
        _timeOfDay += deltaTime * 0.01f;
        if (_timeOfDay > 1f) _timeOfDay = 0f;

        if (_sky != null) _sky.TimeOfDay = _timeOfDay;
    }

    private void HandleWeather()
    {
        if (_rain != null && _camera != null)
        {
            _rain.Position = _camera.Position;

            if (_input.IsKeyPressed(Key.R))
            {
                _rain.Visible = !_rain.Visible;
            }
        }
    }
}
```

## Next Steps

- Add inventory system
- Implement crafting
- Save/load worlds
- Add multiplayer with networking
- Create biomes with different terrain
- Add mobs and AI

Check out the Voxel Plugin source code in `src/Lilly.Voxel.Plugin/` for more advanced features!