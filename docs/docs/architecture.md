# Architecture Guide

This guide explains how Lilly.Engine is structured internally. You'll learn about the layered architecture, how systems communicate, and where to look when you need to extend or modify something.

## Overview

Lilly.Engine follows a clean architecture with clear boundaries between layers. Each layer has a specific responsibility and only depends on layers below it.

```
┌─────────────────────────────────┐
│     Game Layer                  │  Your game code
├─────────────────────────────────┤
│     Plugins (optional)          │  Voxel, UI, etc.
├─────────────────────────────────┤
│     Engine Layer                │  Scene, Audio, Input
├─────────────────────────────────┤
│     Rendering Layer             │  Graphics abstraction
├─────────────────────────────────┤
│     Core Layer                  │  Interfaces, utilities
└─────────────────────────────────┘
```

## The Layers

### Core Layer (`Lilly.Engine.Core`)

This is the foundation. It contains:

**Interfaces** - Contracts that other layers implement:
- `IAssetManager` - Load textures, fonts, shaders
- `IJobSystemService` - Multi-threaded task execution
- `IScriptEngineService` - Script loading and execution
- `IInputManagerService` - Input handling
- `IEventBusService` - Pub/sub messaging

**Base Services** - Abstract implementations and utilities:
- `BaseService` - Service lifecycle management
- Event system infrastructure
- Extension methods for common operations
- JSON serialization contexts

**No Dependencies** - This layer doesn't depend on anything except .NET and a few lightweight libraries.

Why? If you want to replace the rendering system or swap OpenGL for Vulkan, you can do it without touching the core interfaces.

### Rendering Layer (`Lilly.Rendering.Core`)

Graphics abstraction that sits on top of Core. Contains:

**Renderer Interface** - `IRenderer` with implementations like `OpenGlRenderer`
**Display Management** - Window creation, context management
**Camera System** - Base camera classes and interfaces
**Render Pipeline** - Layer-based rendering with priorities

This layer uses TrippyGL (OpenGL wrapper), but it's isolated here. If you wanted DirectX or Vulkan, you'd implement `IRenderer` and swap it out through dependency injection.

### Engine Layer (`Lilly.Engine`)

The main engine. This is where everything comes together:

**Scene Management**
- `SceneManager` - Register and switch scenes
- `BaseScene` - Inherit this for your game scenes
- Scene lifecycle (Initialize → Update → Render → Dispose)

**Asset Management**
- Load assets from embedded resources or files
- Texture caching and atlas support
- Font loading with FontStashSharp
- Shader compilation

**Audio System**
- OpenAL integration
- 3D positional audio
- Sound buffer management

**Input System**
- Keyboard, mouse, gamepad
- Input contexts and bindings
- Event-driven input handling

**Job System**
- Worker thread pool
- Priority queue
- Main thread dispatcher
- Metrics and monitoring

**Services** (17 total):
- AssetManager
- AudioService
- CommandSystemService
- EventBusService
- InputManagerService
- JobSystemService
- NotificationService
- PerformanceProfilerService
- RenderPipeline
- SceneManager
- TimerService
- VersionService
- WindowService
- And more...

### Plugin Layer

Optional modules that extend the engine:

**Lilly.Voxel.Plugin**
- World generation pipeline
- Chunk rendering
- Block registry
- Lighting system

**Lilly.Engine.GameObjects**
- UI controls (buttons, text fields, etc.)
- Quake-style console
- Theme system

**Lilly.Engine.Lua.Scripting**
- MoonSharp integration
- Lua module system
- Script caching and hot reload

Plugins implement `ILillyPlugin` and hook into engine initialization.

### Game Layer (`Lilly.Engine.Game`)

Your actual game. This is where you:
- Register scenes
- Configure the engine
- Define game-specific logic
- Wire up plugins

## Dependency Injection

Everything uses DryIoc for dependency injection. This is how the pieces connect.

### Registration

Services register themselves with the container:

```csharp
// In bootstrap code
container.Register<IAssetManager, AssetManager>(Reuse.Singleton);
container.Register<IJobSystemService, JobSystemService>(Reuse.Singleton);
container.Register<IInputManagerService, InputManagerService>(Reuse.Singleton);
```

### Resolution

When you create a scene, the container injects dependencies:

```csharp
public class MyScene : BaseScene
{
    // Container automatically provides these
    public MyScene(
        IAssetManager assetManager,
        IInputManagerService input,
        IJobSystemService jobSystem)
    {
        // Dependencies are ready to use
    }
}
```

### Why Dependency Injection?

- **Testing** - Mock services for unit tests
- **Flexibility** - Swap implementations without changing code
- **Decoupling** - Code depends on interfaces, not concrete types
- **Clarity** - Constructor shows exactly what a class needs

## Communication Patterns

### Event Bus

Loosely coupled communication between systems:

```csharp
// Publish an event
_eventBus.Publish(new GameOverEvent { Score = 1000 });

// Subscribe to events
_eventBus.Subscribe<GameOverEvent>(OnGameOver);

private void OnGameOver(GameOverEvent evt)
{
    // Handle it
}
```

Used for:
- Input events (KeyPressed, MouseMoved)
- Game events (PlayerDied, LevelComplete)
- System events (WindowResized, AssetLoaded)

### Direct Service Calls

For synchronous operations where you need an immediate result:

```csharp
var texture = _assetManager.LoadTexture("path/to/texture.png");
var isKeyDown = _input.IsKeyDown(Key.Space);
```

Used for:
- Asset loading
- Input queries
- Configuration access

### Job System

For asynchronous work that can run on background threads:

```csharp
_jobSystem.EnqueueJob(() =>
{
    // Runs on worker thread
    var data = GenerateTerrainData();

    // Return to main thread
    _jobSystem.EnqueueJobOnMainThread(() =>
    {
        ApplyTerrainData(data);
    });
});
```

Used for:
- Chunk generation (voxel worlds)
- Asset processing
- Physics calculations
- AI pathfinding

## Core Systems Deep Dive

### Job System

Multi-threaded task execution with priorities.

**Architecture:**
- Worker thread pool (default: CPU core count - 1)
- Priority queue (Normal, High, Critical)
- Main thread dispatcher for Unity-like behavior

**Workflow:**
1. Enqueue job with priority
2. Worker thread picks it up from queue
3. Job executes on worker thread
4. If needed, dispatch back to main thread

**Metrics:**
- Active job count
- Completed job count
- Worker thread utilization
- Queue sizes per priority

See: `src/Lilly.Engine/Services/JobSystemService.cs`

### Render Pipeline

Layer-based rendering system.

**Layers:**
Each layer has a priority (integer). Lower numbers render first (background), higher numbers render last (UI on top).

```csharp
// Background layer (priority 0)
pipeline.RegisterLayer(new BackgroundLayer(), priority: 0);

// Game objects (priority 50)
pipeline.RegisterLayer(new GameLayer(), priority: 50);

// UI (priority 100)
pipeline.RegisterLayer(new UILayer(), priority: 100);
```

**Game Objects:**
Objects register with layers. Layers handle rendering and updates.

```csharp
public class MyLayer : ILayer
{
    public void Update(float deltaTime)
    {
        foreach (var obj in _objects)
            obj.Update(deltaTime);
    }

    public void Render(IRenderer renderer)
    {
        foreach (var obj in _objects)
            obj.Render(renderer);
    }
}
```

**Render Loop:**
1. Clear the screen
2. Update all layers (sorted by priority)
3. Render all layers (sorted by priority)
4. Swap buffers

See: `src/Lilly.Engine/Pipelines/RenderPipeline.cs`

### Scene Management

Scenes are self-contained game states (menu, gameplay, pause screen, etc.).

**Scene Lifecycle:**
```
RegisterScene → ActivateScene → Initialize → Update/Render loop → Deactivate → Dispose
```

**Scene Structure:**
```csharp
public class MyScene : BaseScene
{
    // Called once when scene is activated
    public override void Initialize()
    {
        // Load assets, create game objects
    }

    // Called every frame
    public override void Update(float deltaTime)
    {
        // Update game logic
    }

    // Called every frame after Update
    public override void Render(IRenderer renderer)
    {
        // Custom rendering (optional)
    }

    // Called when scene is deactivated
    public override void Deactivate()
    {
        // Pause, save state
    }

    // Called when scene is disposed
    public override void Dispose()
    {
        // Clean up resources
    }
}
```

**Scene Transitions:**
Switch scenes with optional effects:
- Instant switch
- Fade in/out
- Slide
- Custom transitions

See: `src/Lilly.Engine/Managers/SceneManager.cs`

### Asset Management

Centralized asset loading with caching.

**Embedded Resources:**
Assets are embedded in the assembly:

```xml
<EmbeddedResource Include="Assets\Textures\player.png" />
```

Load with namespace path:
```csharp
var texture = _assetManager.LoadTexture("Lilly.Engine.Assets.Textures.player.png");
```

**Caching:**
Assets are loaded once and cached. Subsequent loads return the cached version.

**Texture Atlas:**
Multiple sprites in one texture:

```csharp
var atlas = _assetManager.LoadTextureAtlas("atlas.png", new[]
{
    new TextureRegion("player", x: 0, y: 0, width: 64, height: 64),
    new TextureRegion("enemy", x: 64, y: 0, width: 64, height: 64)
});

var playerSprite = atlas.GetRegion("player");
```

**Memory Management:**
```csharp
// Unload specific asset
_assetManager.UnloadAsset("path/to/asset.png");

// Unload all unused assets
_assetManager.UnloadUnusedAssets();
```

See: `src/Lilly.Engine/Managers/AssetManager.cs`

### Input System

Multi-source input with bindings.

**Direct Input:**
```csharp
if (_input.IsKeyDown(Key.Space))
    Jump();

var mousePos = _input.GetMousePosition();
var scrollDelta = _input.GetMouseWheelDelta();
```

**Input Bindings:**
Map actions to keys:

```csharp
_input.BindAction("jump", Key.Space);
_input.BindAction("jump", Key.W);  // Multiple bindings

if (_input.IsActionPressed("jump"))
    Jump();
```

**Input Contexts:**
Different control schemes for different game states:

```csharp
// Menu context
var menuContext = new InputContext("menu");
menuContext.BindAction("select", Key.Enter);
menuContext.BindAction("back", Key.Escape);

// Gameplay context
var gameContext = new InputContext("game");
gameContext.BindAction("jump", Key.Space);
gameContext.BindAction("shoot", Key.LeftControl);

// Switch contexts
_input.SetActiveContext("game");
```

**Gamepad Support:**
```csharp
if (_input.IsGamepadButtonDown(GamepadButton.A))
    Jump();

var leftStick = _input.GetGamepadAxis(GamepadAxis.LeftStick);
```

See: `src/Lilly.Engine/Services/InputManagerService.cs`

## Plugin System

Plugins extend the engine without modifying core code.

### Plugin Interface

```csharp
public interface ILillyPlugin
{
    // Metadata
    LillyPluginData GetPluginData();

    // Register services with DI container
    void RegisterModule(IRegistrator registrator);

    // Called after engine services are initialized
    void EngineInitialized(IContainer container);

    // Called when engine is fully ready
    void EngineReady(IContainer container);

    // Global game objects (always rendered)
    IEnumerable<IGameObject> GetGlobalGameObjects(IContainer container);
}
```

### Plugin Example

```csharp
public class MyPlugin : ILillyPlugin
{
    public LillyPluginData GetPluginData() => new()
    {
        Name = "MyPlugin",
        Version = "1.0.0",
        Author = "Your Name",
        Dependencies = new[] { "CorePlugin" }
    };

    public void RegisterModule(IRegistrator registrator)
    {
        // Register services
        registrator.Register<IMyService, MyService>(Reuse.Singleton);
    }

    public void EngineInitialized(IContainer container)
    {
        // Engine is ready, initialize your plugin
        var myService = container.Resolve<IMyService>();
        myService.Initialize();
    }

    public void EngineReady(IContainer container)
    {
        // All plugins loaded, game is starting
    }

    public IEnumerable<IGameObject> GetGlobalGameObjects(IContainer container)
    {
        // Return objects that should always be rendered
        yield return new DebugOverlay();
    }
}
```

### Plugin Registration

In the game bootstrap:

```csharp
var pluginRegistry = new PluginRegistry();
pluginRegistry.RegisterPlugin(new MyPlugin());
pluginRegistry.RegisterPlugin(new VoxelPlugin());
pluginRegistry.RegisterPlugin(new LuaScriptingPlugin());

pluginRegistry.InitializePlugins(container);
```

Plugins can:
- Add services to the DI container
- Register Lua modules
- Add global game objects
- Hook into engine lifecycle
- Depend on other plugins

See: `src/Lilly.Engine/Plugins/`

## Performance Considerations

### Frame Budget

Target 60 FPS = 16.67ms per frame budget:
- **Update:** ~5ms
- **Render:** ~10ms
- **System overhead:** ~1.67ms

Monitor with the Performance Debugger (F1).

### Batching

Sprite rendering uses batching to reduce draw calls:

```csharp
spriteBatcher.Begin();

foreach (var sprite in sprites)
    spriteBatcher.Draw(sprite);

spriteBatcher.End();  // Single draw call
```

### Chunk-based Rendering

Voxel worlds render in chunks (16x256x16 blocks):
- Only visible chunks are rendered
- Mesh is pre-built and cached
- Face culling reduces triangle count
- Lighting is pre-computed per chunk

### Job System

Move expensive work off the main thread:
- Terrain generation
- Mesh building
- Asset loading
- AI calculations

Use `JobSystemService` to parallelize work across CPU cores.

## Debugging

### Built-in Debuggers

Enable with function keys:

**F1 - Performance Debugger**
- Frame time graph
- FPS counter
- Memory usage
- GC collections

**F2 - Job System Debugger**
- Worker thread status
- Queue sizes (Normal, High, Critical)
- Completed job count
- Recent job history

**F3 - Render Pipeline Debugger**
- Layer list with priorities
- Game object counts per layer
- Active camera info

**F4 - Camera Debugger**
- Position and rotation
- Projection matrix
- View matrix
- Frustum planes

### Logging

Use Serilog through the logger interface:

```csharp
_logger.Debug("Loading texture: {Path}", path);
_logger.Information("Scene {Name} initialized", sceneName);
_logger.Warning("Asset cache is {Percent}% full", percent);
_logger.Error(exception, "Failed to load {Asset}", assetPath);
```

Logs go to:
- Console output
- File (if configured)
- Custom sinks

### Notifications

In-game toast notifications:

```csharp
_notifications.Show("Settings saved", NotificationLevel.Info);
_notifications.Show("Low FPS detected", NotificationLevel.Warning);
_notifications.Show("Connection lost", NotificationLevel.Error);
```

## Extension Points

Where to hook in when you need to extend the engine:

**Custom Renderer**
→ Implement `IRenderer` in Lilly.Rendering.Core

**Custom Camera**
→ Inherit from `Base3dCamera` or `OrthographicCamera`

**Custom Game Object**
→ Implement `IGameObject` or inherit from base classes

**Custom Service**
→ Create interface in Core, implement in Engine, register with DI

**Custom Plugin**
→ Implement `ILillyPlugin`

**Custom Lua Module**
→ Implement `ILuaModule` and register with script engine

**Custom Shader**
→ Add shader files to Assets, load with AssetManager

**Custom Scene Transition**
→ Extend `BaseSceneTransition`

## What's Next?

Now that you understand the architecture:

- **[Plugin Development](plugin-development.md)** - Build your own plugin
- **[Lua Scripting](lua-scripting.md)** - Learn the Lua module system
- **[Tutorials](tutorials/)** - Build complete features
- **[API Reference](../api/)** - Explore the codebase

Questions? Check the source code - it's well-documented and designed to be readable.