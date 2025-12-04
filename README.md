# Lilly.Engine

A modern, modular game engine built in C# for .NET. Designed for developers who want to create 2D and 3D games with a clean architecture and extensible plugin system.

## Features

- **Modular plugin architecture** - Use only what you need, extend the rest
- **Voxel world generation** - Build Minecraft-style worlds with custom terrain generation
- **Lua scripting** - Script game logic without recompiling
- **Hardware-accelerated rendering** - OpenGL-based rendering with sprite batching
- **Job system** - Multi-threaded task execution for better performance
- **Comprehensive UI framework** - Buttons, text fields, combo boxes, and custom controls
- **Audio system** - 3D positional audio with OpenAL
- **Input handling** - Keyboard, mouse, and gamepad support with customizable bindings
- **Built-in debuggers** - Visualize job system, rendering pipeline, and performance metrics

## Quick Start

### Requirements

- .NET 10.0 SDK or later
- OpenGL 4.0+ capable GPU

### Building

```bash
git clone https://github.com/yourusername/Lilly.Engine.git
cd Lilly.Engine
dotnet build
```

### Running the demo

```bash
cd src/Lilly.Engine.Game
dotnet run
```

## Architecture

The engine is organized into several layers:

- **Lilly.Engine.Core** - Foundation layer with interfaces and core utilities
- **Lilly.Rendering.Core** - Graphics rendering abstraction
- **Lilly.Engine** - Main engine with rendering, audio, input, and scene management
- **Lilly.Engine.GameObjects** - UI controls and game object implementations
- **Lilly.Voxel.Plugin** - Complete voxel world generation and rendering
- **Lilly.Engine.Lua.Scripting** - Lua integration using MoonSharp
- **Lilly.Engine.Game** - Entry point and application bootstrap

Everything is wired together using dependency injection (DryIoc), making it easy to swap implementations or add your own.

## Creating a Simple Scene

Here's a minimal example to get started:

```csharp
public class MyScene : BaseScene
{
    private readonly IAssetManager _assetManager;

    public MyScene(IAssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    public override void Initialize()
    {
        // Load a texture
        var texture = _assetManager.LoadTexture("path/to/texture.png");

        // Create a game object
        var sprite = new TextureGameObject(texture)
        {
            Position = new Vector2(100, 100)
        };

        // Add to the scene
        AddGameObject(sprite);
    }
}
```

## Scripting with Lua

Scripts have full access to the engine core, input, camera, and UI. The engine automatically generates a `definitions.lua` file for IDE autocompletion.

```lua
-- scripts/init.lua

function on_initialize()
    window.set_title("My Game")
    camera.register_fps("main_cam")
    camera.set_active("main_cam")
end

-- Input handling
input_manager.bind_key("Space", function()
    notifications.info("Jump!")
end)

input_manager.bind_mouse(function(xDelta, yDelta, posX, posY)
    local sensitivity = 0.003
    camera.dispatch_mouse_fps(yDelta * sensitivity, xDelta * sensitivity)
end)

-- Update loop
engine.on_update(function(dt)
    if input_manager.is_key_down("W") then
        camera.dispatch_keyboard_fps(1, 0, 0)
    end
end)

-- UI with ImGui
engine.on_update(function(dt)
    if im_gui.begin_window("Debug") then
        im_gui.text("FPS: " .. (1/dt))
        im_gui.end_window()
    end
end)
```

## Voxel Worlds

The `Lilly.Voxel.Plugin` provides a highly configurable world generation pipeline. You can inject steps for heightmaps, erosion, caves, and decoration.

```csharp
// In your plugin's EngineReady method
public void EngineReady(IContainer container)
{
    var generator = container.Resolve<IChunkGeneratorService>();
    var blockRegistry = container.Resolve<IBlockRegistry>();
    
    // Configure the pipeline
    generator.AddGeneratorStep(new HeightMapGenerationStep());
    generator.AddGeneratorStep(new TerrainErosionGenerationStep());
    generator.AddGeneratorStep(new CaveGenerationStep());
    generator.AddGeneratorStep(new DecorationGenerationStep(blockRegistry));
    
    // Lighting must run last
    var lighting = container.Resolve<ChunkLightPropagationService>();
    generator.AddGeneratorStep(new LightingGenerationStep(lighting));
}
```

## Plugins

Create plugins to extend the engine with new systems or game content.

```csharp
public class MyPlugin : ILillyPlugin
{
    public LillyPluginData LillyData => new(
        id: "com.example.myplugin",
        name: "My Custom Plugin",
        version: "1.0.0",
        author: "Me",
        dependencies: "com.tgiachi.lilly.voxel" // Optional dependencies
    );

    public IContainer RegisterModule(IContainer container)
    {
        // Register services and game objects
        container.RegisterService<IMyService, MyService>();
        container.RegisterGameObject<MyCustomEntity>();
        
        // Register script modules to expose C# to Lua
        container.RegisterScriptModule<MyScriptModule>();
        
        return container;
    }

    public void EngineInitialized(IContainer container) { }

    public void EngineReady(IContainer container)
    {
        // Engine is fully loaded, safe to access all services
        var assets = container.Resolve<IAssetManager>();
        assets.LoadTexture("my_texture", "path/to/texture.png");
    }

    public IEnumerable<IGameObject> GetGlobalGameObjects(IGameObjectFactory factory)
    {
        // Automatically add objects to the scene
        yield return factory.Create<MyCustomEntity>();
    }
}
```

## Documentation

Full documentation is available at [your-docs-url] including:

- [Getting Started Guide](docs/docs/getting-started.md)
- [Architecture Overview](docs/docs/architecture.md)
- [Plugin Development](docs/docs/plugin-development.md)
- [Lua Scripting Reference](docs/docs/lua-scripting.md)
- [API Reference](docs/api/)

## Building Documentation

Documentation is built using DocFX:

```bash
cd docs
dotnet tool restore
dotnet docfx docfx.json --serve
```

Then open http://localhost:8080 in your browser.

## Project Status

This is an active personal project that has grown into a fully-featured engine. The core systems are stable, but expect some API changes as development continues.

### What's Working

- Core rendering pipeline with 2D/3D support
- Complete voxel world generation with lighting
- Lua scripting with hot reload
- UI system with multiple controls
- Audio system with 3D positioning
- Job system with thread pooling
- Scene management and transitions

### Planned Features

- Networking/multiplayer support
- Physics integration
- More example games
- Visual editor
- Better documentation and tutorials

## Contributing

Contributions are welcome! Whether it's bug reports, feature requests, or pull requests, feel free to get involved.

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

Built with these excellent libraries:

- [TrippyGL](https://github.com/SilkCommunity/TrippyGL) - OpenGL wrapper
- [DryIoc](https://github.com/dadhi/DryIoc) - Dependency injection
- [MoonSharp](https://www.moonsharp.org/) - Lua interpreter for .NET
- [FontStashSharp](https://github.com/FontStashSharp/FontStashSharp) - Font rendering
- [Silk.NET](https://github.com/dotnet/Silk.NET) - Windowing and input
- [OpenAL](https://www.openal.org/) - Audio
