# Getting Started

This guide walks you through setting up Lilly.Engine and creating your first scene. By the end, you'll have a working project that renders sprites and responds to input.

## Prerequisites

You'll need:

- **.NET 10.0 SDK** or later - [Download here](https://dotnet.microsoft.com/download)
- **GPU with OpenGL 4.0+ support** - Most modern GPUs work fine
- **A code editor** - Visual Studio, Rider, or VS Code with C# extension

To verify your .NET installation:

```bash
dotnet --version
# Should output 10.0.0 or higher
```

## Cloning and Building

Get the source code:

```bash
git clone https://github.com/yourusername/Lilly.Engine.git
cd Lilly.Engine
```

Build the solution:

```bash
dotnet build
```

This compiles all projects including the core engine, rendering layer, plugins, and the demo game.

Run the demo to make sure everything works:

```bash
cd src/Lilly.Engine.Game
dotnet run
```

You should see a window open with the engine running. Press `Escape` to close it.

## Project Structure

Before building anything, let's understand the layout:

```
Lilly.Engine/
├── src/
│   ├── Lilly.Engine.Core/          # Foundation layer
│   ├── Lilly.Rendering.Core/       # Graphics abstraction
│   ├── Lilly.Engine/               # Main engine
│   ├── Lilly.Engine.GameObjects/   # UI controls
│   ├── Lilly.Voxel.Plugin/         # Voxel world system
│   ├── Lilly.Engine.Lua.Scripting/ # Lua integration
│   └── Lilly.Engine.Game/          # Entry point
├── docs/                            # Documentation (you're here!)
└── Lilly.Engine.sln                # Solution file
```

## Creating Your First Scene

Let's build a simple scene that displays a sprite and moves it with keyboard input.

### Step 1: Create a New Scene Class

In `Lilly.Engine.Game`, create a new file called `MyFirstScene.cs`:

```csharp
using Lilly.Engine.Core.Interfaces;
using Lilly.Engine.Scenes;
using Lilly.Engine.GameObjects;
using Silk.NET.Maths;
using System.Drawing;

namespace Lilly.Engine.Game;

public class MyFirstScene : BaseScene
{
    private readonly IAssetManager _assetManager;
    private readonly IInputManagerService _input;
    private TextureGameObject? _player;

    private const float PlayerSpeed = 200f;

    public MyFirstScene(
        IAssetManager assetManager,
        IInputManagerService input)
    {
        _assetManager = assetManager;
        _input = input;
    }

    public override void Initialize()
    {
        // Create a simple colored rectangle as our player
        _player = new TextureGameObject(null)
        {
            Position = new Vector2D<float>(400, 300),
            Size = new Vector2D<float>(50, 50),
            Color = Color.Blue
        };

        AddGameObject(_player);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        if (_player == null) return;

        // Handle input
        var movement = Vector2D<float>.Zero;

        if (_input.IsKeyDown(Silk.NET.Input.Key.W))
            movement.Y -= 1;
        if (_input.IsKeyDown(Silk.NET.Input.Key.S))
            movement.Y += 1;
        if (_input.IsKeyDown(Silk.NET.Input.Key.A))
            movement.X -= 1;
        if (_input.IsKeyDown(Silk.NET.Input.Key.D))
            movement.X += 1;

        // Normalize diagonal movement
        if (movement.LengthSquared > 0)
        {
            var length = MathF.Sqrt(movement.X * movement.X + movement.Y * movement.Y);
            movement.X /= length;
            movement.Y /= length;
        }

        // Apply movement
        _player.Position += movement * PlayerSpeed * deltaTime;
    }
}
```

### Step 2: Register Your Scene

Open `Program.cs` in `Lilly.Engine.Game` and register your scene:

```csharp
// Find the scene registration section and add:
sceneManager.RegisterScene<MyFirstScene>("MyFirstScene");

// Then activate it:
sceneManager.ActivateScene("MyFirstScene");
```

### Step 3: Run It

```bash
dotnet run
```

You should see a blue square that moves when you press WASD keys.

## Adding a Texture

Let's replace that blue square with an actual image.

### Step 1: Add Your Texture

Place an image file (PNG or JPG) in `src/Lilly.Engine/Assets/Textures/` - let's call it `player.png`.

Make sure the file is set as an **Embedded Resource** in the csproj:

```xml
<ItemGroup>
  <EmbeddedResource Include="Assets\Textures\player.png" />
</ItemGroup>
```

### Step 2: Load the Texture

Update your scene's `Initialize` method:

```csharp
public override void Initialize()
{
    // Load texture from embedded resources
    var texture = _assetManager.LoadTexture("Lilly.Engine.Assets.Textures.player.png");

    _player = new TextureGameObject(texture)
    {
        Position = new Vector2D<float>(400, 300),
        Size = new Vector2D<float>(64, 64)
    };

    AddGameObject(_player);
}
```

Note the resource path format: `Namespace.Folder.Folder.Filename`

## Adding Text

Let's add a score counter to the scene.

```csharp
private TextGameObject? _scoreText;
private int _score = 0;

public override void Initialize()
{
    // ... existing player code ...

    // Load a font
    var font = _assetManager.LoadFont("Lilly.Engine.Assets.Fonts.Roboto-Regular.ttf", 24);

    _scoreText = new TextGameObject(font)
    {
        Position = new Vector2D<float>(10, 10),
        Text = "Score: 0",
        Color = Color.White
    };

    AddGameObject(_scoreText);
}

public override void Update(float deltaTime)
{
    base.Update(deltaTime);

    // Increase score over time
    _score += (int)(deltaTime * 10);

    if (_scoreText != null)
        _scoreText.Text = $"Score: {_score}";

    // ... rest of update code ...
}
```

## Using the Job System

The engine includes a job system for multi-threaded work. Here's how to use it:

```csharp
private readonly IJobSystemService _jobSystem;

// In constructor:
public MyFirstScene(
    IAssetManager assetManager,
    IInputManagerService input,
    IJobSystemService jobSystem)
{
    _assetManager = assetManager;
    _input = input;
    _jobSystem = jobSystem;
}

// Use it:
public void DoExpensiveWork()
{
    _jobSystem.EnqueueJob(() =>
    {
        // This runs on a worker thread
        var result = CalculateSomethingExpensive();

        // Return to main thread for UI updates
        _jobSystem.EnqueueJobOnMainThread(() =>
        {
            UpdateUIWithResult(result);
        });
    });
}
```

## Working with Cameras

### 2D Orthographic Camera

By default, scenes use an orthographic camera. You can customize it:

```csharp
public override void Initialize()
{
    // Access the active camera
    var camera = Camera as OrthographicCamera;

    if (camera != null)
    {
        // Set custom bounds
        camera.Left = 0;
        camera.Right = 1920;
        camera.Bottom = 1080;
        camera.Top = 0;
    }

    // ... rest of initialization ...
}
```

### 3D Camera

For 3D scenes, switch to a perspective camera:

```csharp
using Lilly.Engine.Cameras;

public override void Initialize()
{
    // Create a free-moving camera
    var camera3d = new FreeCamera(
        position: new Vector3D<float>(0, 5, 10),
        aspectRatio: 16f / 9f
    );

    // Replace the scene's camera
    Camera = camera3d;
}
```

## Handling Events

Use the event bus to communicate between systems:

```csharp
private readonly IEventBusService _eventBus;

public MyFirstScene(
    IAssetManager assetManager,
    IInputManagerService input,
    IEventBusService eventBus)
{
    _assetManager = assetManager;
    _input = input;
    _eventBus = eventBus;
}

public override void Initialize()
{
    // Subscribe to events
    _eventBus.Subscribe<PlayerHitEvent>(OnPlayerHit);

    // ... rest of initialization ...
}

private void OnPlayerHit(PlayerHitEvent evt)
{
    // Handle the event
    _score -= 10;
}

// Publish events
private void CheckCollision()
{
    if (IsColliding())
    {
        _eventBus.Publish(new PlayerHitEvent());
    }
}

public override void Dispose()
{
    // Unsubscribe from events
    _eventBus.Unsubscribe<PlayerHitEvent>(OnPlayerHit);
    base.Dispose();
}
```

## Scene Transitions

Switch between scenes with optional effects:

```csharp
private readonly ISceneManager _sceneManager;

// To switch scenes:
_sceneManager.ActivateScene("GameOverScene");

// With a fade effect:
_sceneManager.ActivateSceneWithTransition(
    "GameOverScene",
    TransitionEffect.Fade,
    duration: 1.0f
);
```

## Using Lua Scripts

Lua scripts let you modify game behavior without recompiling. The engine loads `scripts/init.lua` as the entry point.

Create `scripts/init.lua`:

```lua
-- scripts/init.lua

function on_initialize()
    window.set_title("My First Game")
    window.set_vsync(true)

    console.log("Game initialized!")
end

-- Bind input
input_manager.bind_key("Escape", function()
    console.log("Escape pressed!")
end)

-- Update callback
engine.on_update(function(game_time)
    -- Called every frame
    -- game_time.elapsed_game_time is delta time
end)
```

The script runs automatically. For complete Lua API documentation, see the [Lua Scripting Guide](lua-scripting.md).

## Debugging

### Enable Built-in Debuggers

Press these keys at runtime:

- **F1** - Performance debugger (FPS, frame times)
- **F2** - Job system debugger (worker threads, queue status)
- **F3** - Render pipeline debugger (layers, object counts)
- **F4** - Camera debugger (position, rotation, projection)

### Console Output

Use the logging service:

```csharp
private readonly ILogger _logger;

_logger.Information("Scene initialized");
_logger.Warning("Low health: {Health}", playerHealth);
_logger.Error("Failed to load texture: {Path}", texturePath);
```

### Notifications

Show in-game notifications:

```csharp
private readonly INotificationService _notifications;

_notifications.Show("Level Complete!", NotificationLevel.Success);
_notifications.Show("Low ammo", NotificationLevel.Warning);
_notifications.Show("Player died", NotificationLevel.Error);
```

## Building for Release

Development builds include debugging symbols and verbose logging. For release builds:

```bash
dotnet build -c Release
```

This:
- Enables XML documentation generation
- Removes debug symbols
- Optimizes code
- Reduces binary size

## Next Steps

Now that you have a working scene, explore these topics:

- **[Architecture Guide](architecture.md)** - Understand how systems interact
- **[Plugin Development](plugin-development.md)** - Extend the engine with custom plugins
- **[Lua Scripting](lua-scripting.md)** - Write game logic in Lua
- **[Voxel Tutorial](tutorials/voxel-world.md)** - Build a Minecraft-style world
- **[UI Tutorial](tutorials/custom-ui.md)** - Create custom UI controls

## Common Issues

### "Could not load file or assembly"

Make sure all projects are built. Run `dotnet build` from the solution root.

### Black screen on startup

Check that your GPU supports OpenGL 4.0+. Update your graphics drivers.

### Scripts not loading

Verify script paths are relative to the executable. Use `File.Exists()` to debug paths.

### High memory usage

The asset manager caches loaded resources. Call `_assetManager.UnloadUnusedAssets()` periodically.

## Getting Help

- **GitHub Issues** - Report bugs or request features
- **Documentation** - Read the guides and API reference
- **Source Code** - The codebase is well-documented, dive in!

Happy game development!