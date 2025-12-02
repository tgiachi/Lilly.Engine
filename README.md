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

Scripts can hook into engine events and control game objects:

```lua
-- scripts/player.lua

local player_speed = 5.0

function on_update(delta_time)
    local input = engine.input

    if input:is_key_down("W") then
        player.position.y = player.position.y + player_speed * delta_time
    end
end

function on_collision(other)
    notifications.show("Hit something!", "Info")
end
```

Register and run the script:

```csharp
var script = scriptEngine.LoadScript("scripts/player.lua");
script.Call("on_update", deltaTime);
```

## Voxel Worlds

The voxel plugin includes a complete world generation pipeline:

```csharp
// Register custom block types
blockRegistry.Register(new BlockType
{
    Id = 100,
    Name = "custom_stone",
    IsSolid = true,
    Textures = new BlockTextureSet
    {
        Top = "blocks/stone_top.png",
        Sides = "blocks/stone_side.png",
        Bottom = "blocks/stone_bottom.png"
    }
});

// Generate world with custom settings
var world = new WorldGameObject(generationSettings);
world.Generate();
```

## Plugins

Creating a plugin is straightforward:

```csharp
public class MyPlugin : ILillyPlugin
{
    public LillyPluginData GetPluginData() => new()
    {
        Name = "MyPlugin",
        Version = "1.0.0",
        Author = "Your Name"
    };

    public void RegisterModule(IRegistrator registrator)
    {
        // Register services, singletons, etc.
        registrator.Register<IMyService, MyService>(Reuse.Singleton);
    }

    public void EngineInitialized(IContainer container)
    {
        // Engine is ready, initialize your plugin
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
