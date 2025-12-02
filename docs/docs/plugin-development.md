# Plugin Development Guide

Plugins are how you extend Lilly.Engine without modifying the core codebase. This guide shows you how to build your own plugin from scratch.

## What Plugins Can Do

Plugins have access to the full engine lifecycle:

- Register services with dependency injection
- Add global game objects (like debug overlays)
- Hook into engine initialization
- Expose Lua modules for scripting
- Depend on other plugins
- Provide custom assets

## Plugin Interface

Every plugin implements `ILillyPlugin`:

```csharp
public interface ILillyPlugin
{
    LillyPluginData GetPluginData();
    void RegisterModule(IRegistrator registrator);
    void EngineInitialized(IContainer container);
    void EngineReady(IContainer container);
    IEnumerable<IGameObject> GetGlobalGameObjects(IContainer container);
}
```

## Creating Your First Plugin

Let's build a simple plugin that adds a particle system to the engine.

### Step 1: Create the Project

```bash
cd src/
dotnet new classlib -n Lilly.Particles.Plugin
cd Lilly.Particles.Plugin
```

### Step 2: Add References

Edit `Lilly.Particles.Plugin.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../Lilly.Engine.Core/Lilly.Engine.Core.csproj" />
    <ProjectReference Include="../Lilly.Engine/Lilly.Engine.csproj" />
    <ProjectReference Include="../Lilly.Rendering.Core/Lilly.Rendering.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="DryIoc" Version="6.0.0" />
  </ItemGroup>
</Project>
```

### Step 3: Implement the Plugin

Create `ParticlesPlugin.cs`:

```csharp
using DryIoc;
using Lilly.Engine.Core.Interfaces;
using Lilly.Engine.Plugins;
using Lilly.Engine.Types;
using Lilly.Rendering.Core.Interfaces;

namespace Lilly.Particles.Plugin;

public class ParticlesPlugin : ILillyPlugin
{
    public LillyPluginData GetPluginData() => new()
    {
        Name = "Particles",
        Version = "1.0.0",
        Author = "Your Name",
        Description = "Particle system for visual effects",
        Dependencies = Array.Empty<string>()
    };

    public void RegisterModule(IRegistrator registrator)
    {
        // Register services
        registrator.Register<IParticleSystemService, ParticleSystemService>(
            Reuse.Singleton);
    }

    public void EngineInitialized(IContainer container)
    {
        // Initialize the particle system
        var particleSystem = container.Resolve<IParticleSystemService>();
        particleSystem.Initialize();
    }

    public void EngineReady(IContainer container)
    {
        // Engine is fully ready
    }

    public IEnumerable<IGameObject> GetGlobalGameObjects(IContainer container)
    {
        // No global objects needed
        yield break;
    }
}
```

### Step 4: Create the Service Interface

Create `IParticleSystemService.cs`:

```csharp
namespace Lilly.Particles.Plugin;

public interface IParticleSystemService
{
    void Initialize();
    ParticleEmitter CreateEmitter(ParticleEmitterConfig config);
    void Update(float deltaTime);
    void Render(IRenderer renderer);
}
```

### Step 5: Implement the Service

Create `ParticleSystemService.cs`:

```csharp
using Lilly.Engine.Core.Interfaces;
using Lilly.Rendering.Core.Interfaces;
using System.Collections.Generic;

namespace Lilly.Particles.Plugin;

public class ParticleSystemService : IParticleSystemService
{
    private readonly List<ParticleEmitter> _emitters = new();
    private readonly IJobSystemService _jobSystem;

    public ParticleSystemService(IJobSystemService jobSystem)
    {
        _jobSystem = jobSystem;
    }

    public void Initialize()
    {
        // Setup particle pools, load shaders, etc.
    }

    public ParticleEmitter CreateEmitter(ParticleEmitterConfig config)
    {
        var emitter = new ParticleEmitter(config);
        _emitters.Add(emitter);
        return emitter;
    }

    public void Update(float deltaTime)
    {
        // Update all emitters in parallel
        _jobSystem.EnqueueJob(() =>
        {
            foreach (var emitter in _emitters)
            {
                emitter.Update(deltaTime);
            }
        });
    }

    public void Render(IRenderer renderer)
    {
        foreach (var emitter in _emitters)
        {
            emitter.Render(renderer);
        }
    }
}
```

### Step 6: Register the Plugin

In `Lilly.Engine.Game/Program.cs`:

```csharp
var pluginRegistry = new PluginRegistry();
pluginRegistry.RegisterPlugin(new ParticlesPlugin());
// ... other plugins ...

pluginRegistry.InitializePlugins(container);
```

### Step 7: Use Your Plugin

In any scene:

```csharp
public class MyScene : BaseScene
{
    private readonly IParticleSystemService _particles;

    public MyScene(IParticleSystemService particles)
    {
        _particles = particles;
    }

    public override void Initialize()
    {
        var emitter = _particles.CreateEmitter(new ParticleEmitterConfig
        {
            Position = new Vector3D<float>(0, 10, 0),
            EmissionRate = 100,
            Lifetime = 2.0f,
            Color = Color.Orange
        });
    }
}
```

## Plugin with Global Game Objects

Some plugins need objects that are always rendered (like debug overlays). Here's how:

```csharp
public class DebugPlugin : ILillyPlugin
{
    public LillyPluginData GetPluginData() => new()
    {
        Name = "Debug Overlay",
        Version = "1.0.0",
        Author = "Your Name"
    };

    public void RegisterModule(IRegistrator registrator)
    {
        // No services needed
    }

    public void EngineInitialized(IContainer container)
    {
        // Nothing to initialize
    }

    public void EngineReady(IContainer container)
    {
        // Nothing to do
    }

    public IEnumerable<IGameObject> GetGlobalGameObjects(IContainer container)
    {
        // These objects are always rendered on top
        var assetManager = container.Resolve<IAssetManager>();
        var font = assetManager.LoadFont("path/to/font.ttf", 16);

        yield return new FpsCounter(font)
        {
            Position = new Vector2D<float>(10, 10)
        };

        yield return new MemoryMonitor(font)
        {
            Position = new Vector2D<float>(10, 30)
        };
    }
}
```

Global objects:
- Always render on top of everything
- Survive scene changes
- Perfect for debug tools, HUDs, notifications

## Adding Lua Modules

Expose your plugin to Lua scripts.

### Step 1: Create a Lua Module

```csharp
using Lilly.Engine.Lua.Scripting.Interfaces;
using MoonSharp.Interpreter;

namespace Lilly.Particles.Plugin.Lua;

public class ParticlesLuaModule : ILuaModule
{
    private readonly IParticleSystemService _particleSystem;

    public ParticlesLuaModule(IParticleSystemService particleSystem)
    {
        _particleSystem = particleSystem;
    }

    public string ModuleName => "particles";

    public void RegisterModule(Script script)
    {
        var table = new Table(script);

        // Register functions
        table["spawn"] = (Action<double, double, double>)SpawnParticles;
        table["clear_all"] = (Action)ClearAll;

        script.Globals[ModuleName] = table;
    }

    private void SpawnParticles(double x, double y, double z)
    {
        _particleSystem.CreateEmitter(new ParticleEmitterConfig
        {
            Position = new Vector3D<float>((float)x, (float)y, (float)z),
            EmissionRate = 50,
            Lifetime = 1.0f
        });
    }

    private void ClearAll()
    {
        // Clear all emitters
    }
}
```

### Step 2: Register the Module

In your plugin:

```csharp
public void EngineInitialized(IContainer container)
{
    var scriptEngine = container.Resolve<IScriptEngineService>();
    var particleModule = new ParticlesLuaModule(
        container.Resolve<IParticleSystemService>());

    scriptEngine.RegisterModule(particleModule);
}
```

### Step 3: Use in Lua

```lua
-- script.lua

function on_explosion(x, y, z)
    particles.spawn(x, y, z)
end

function on_reset()
    particles.clear_all()
end
```

## Plugin Dependencies

Your plugin might depend on other plugins:

```csharp
public LillyPluginData GetPluginData() => new()
{
    Name = "Advanced Particles",
    Version = "2.0.0",
    Author = "Your Name",
    Dependencies = new[] { "Particles", "Physics" }
};
```

The engine ensures dependencies load first. If a dependency is missing, the plugin won't load and you'll get an error.

## Assets in Plugins

Include assets (textures, shaders, sounds) in your plugin.

### Step 1: Add Assets to Project

```
Lilly.Particles.Plugin/
├── Assets/
│   ├── Textures/
│   │   └── particle.png
│   └── Shaders/
│       └── particle.glsl
├── ParticlesPlugin.cs
└── Lilly.Particles.Plugin.csproj
```

### Step 2: Mark as Embedded Resources

```xml
<ItemGroup>
  <EmbeddedResource Include="Assets\**\*" />
</ItemGroup>
```

### Step 3: Load Assets

```csharp
var texture = _assetManager.LoadTexture(
    "Lilly.Particles.Plugin.Assets.Textures.particle.png");

var shader = _assetManager.LoadShader(
    "Lilly.Particles.Plugin.Assets.Shaders.particle.glsl");
```

## Testing Your Plugin

Create a test scene that uses your plugin:

```csharp
public class ParticleTestScene : BaseScene
{
    private readonly IParticleSystemService _particles;
    private readonly IInputManagerService _input;

    public ParticleTestScene(
        IParticleSystemService particles,
        IInputManagerService input)
    {
        _particles = particles;
        _input = input;
    }

    public override void Initialize()
    {
        // Create test emitters
        _particles.CreateEmitter(new ParticleEmitterConfig
        {
            Position = Vector3D<float>.Zero,
            EmissionRate = 100,
            Lifetime = 2.0f
        });
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        // Spawn particles on click
        if (_input.IsMouseButtonPressed(MouseButton.Left))
        {
            var mousePos = _input.GetMousePosition();
            _particles.CreateEmitter(new ParticleEmitterConfig
            {
                Position = new Vector3D<float>(mousePos.X, mousePos.Y, 0),
                EmissionRate = 50,
                Lifetime = 1.0f
            });
        }

        _particles.Update(deltaTime);
    }

    public override void Render(IRenderer renderer)
    {
        base.Render(renderer);
        _particles.Render(renderer);
    }
}
```

## Real-World Example: Voxel Plugin

The voxel plugin is a complete example. It includes:

**Services:**
- `ChunkGeneratorService` - Generates terrain
- `ChunkMeshBuilder` - Builds chunk meshes
- `ChunkLightingService` - Calculates lighting
- `BlockRegistry` - Manages block types

**Lua Modules:**
- `WorldModule` - Control world generation
- `BlockRegistryModule` - Register custom blocks
- `GenerationModule` - Custom generation steps

**Assets:**
- Block textures
- Shaders for voxel rendering

**Game Objects:**
- `WorldGameObject` - Renders the world
- `ChunkGameObject` - Individual chunks
- `BlockOutlineGameObject` - Block selection

Check the source: `src/Lilly.Voxel.Plugin/`

## Best Practices

### Keep Plugins Focused

One plugin, one responsibility. Don't make a "everything" plugin.

Good:
- Particle system plugin
- Physics plugin
- Networking plugin

Bad:
- Game mechanics plugin (too vague)

### Use Interfaces

Define interfaces in your plugin, implement them, register with DI. This lets users mock or replace your implementation.

```csharp
// Interface
public interface IParticleSystemService { }

// Implementation
public class ParticleSystemService : IParticleSystemService { }

// Registration
registrator.Register<IParticleSystemService, ParticleSystemService>();
```

### Document Public APIs

Add XML documentation to public classes and methods:

```csharp
/// <summary>
/// Creates a new particle emitter at the specified position.
/// </summary>
/// <param name="config">Configuration for the emitter</param>
/// <returns>The created emitter instance</returns>
public ParticleEmitter CreateEmitter(ParticleEmitterConfig config)
{
    // ...
}
```

### Handle Initialization Errors

Don't crash the engine if your plugin fails to load:

```csharp
public void EngineInitialized(IContainer container)
{
    try
    {
        var service = container.Resolve<IParticleSystemService>();
        service.Initialize();
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Failed to initialize particle system");
        // Plugin is disabled but engine continues
    }
}
```

### Version Your Plugin

Use semantic versioning (major.minor.patch):

```csharp
public LillyPluginData GetPluginData() => new()
{
    Name = "Particles",
    Version = "1.2.3",  // Major.Minor.Patch
    // ...
};
```

- Major: Breaking changes
- Minor: New features, backward compatible
- Patch: Bug fixes

## Plugin Lifecycle

Understanding when each method is called:

```
1. GetPluginData()
   - Called first
   - Engine reads metadata
   - Checks dependencies

2. RegisterModule(registrator)
   - Register services with DI
   - No container resolution yet

3. EngineInitialized(container)
   - Core engine services are ready
   - You can resolve and initialize services
   - Other plugins may not be ready yet

4. EngineReady(container)
   - All plugins loaded
   - Game is about to start
   - Final setup here

5. GetGlobalGameObjects(container)
   - Called once after EngineReady
   - Return objects that should always render
```

## Distributing Your Plugin

### As Source

Users add your project to the solution:

```bash
git clone https://github.com/yourname/Lilly.YourPlugin.git src/Lilly.YourPlugin/
dotnet sln add src/Lilly.YourPlugin/Lilly.YourPlugin.csproj
```

### As NuGet Package

Build and publish a NuGet package:

```bash
dotnet pack -c Release
dotnet nuget push bin/Release/Lilly.YourPlugin.1.0.0.nupkg
```

Users install via NuGet:

```bash
dotnet add package Lilly.YourPlugin
```

### As DLL

Build and share the DLL:

```bash
dotnet build -c Release
# Share bin/Release/net10.0/Lilly.YourPlugin.dll
```

Users reference the DLL:

```xml
<ItemGroup>
  <Reference Include="Lilly.YourPlugin">
    <HintPath>path/to/Lilly.YourPlugin.dll</HintPath>
  </Reference>
</ItemGroup>
```

## Debugging Plugins

### Enable Debug Logging

```csharp
public void EngineInitialized(IContainer container)
{
    var logger = container.Resolve<ILogger>();
    logger.Debug("Particle plugin initializing...");

    // Your initialization code

    logger.Debug("Particle plugin ready");
}
```

### Use Debuggers

The engine includes debuggers (F1-F4). Add your own:

```csharp
public class ParticleDebugger : IGameObject
{
    private readonly IParticleSystemService _particles;

    public void Render(IRenderer renderer)
    {
        ImGui.Begin("Particle System");
        ImGui.Text($"Active emitters: {_particles.EmitterCount}");
        ImGui.Text($"Particle count: {_particles.TotalParticles}");
        // ... more debug info ...
        ImGui.End();
    }
}
```

Return it from `GetGlobalGameObjects()` when debug mode is enabled.

### Unit Tests

Test your plugin in isolation:

```csharp
[Test]
public void CreateEmitter_ShouldAddToList()
{
    var jobSystem = new MockJobSystemService();
    var particleSystem = new ParticleSystemService(jobSystem);

    var emitter = particleSystem.CreateEmitter(new ParticleEmitterConfig());

    Assert.AreEqual(1, particleSystem.EmitterCount);
}
```

## Common Patterns

### Service with Update Loop

```csharp
public class MyService : IMyService, IUpdatable
{
    public void Update(float deltaTime)
    {
        // Called every frame
    }
}
```

Register as singleton and add to update list.

### Service with Background Processing

```csharp
public class MyService : IMyService
{
    private readonly IJobSystemService _jobSystem;

    public void ProcessAsync(Data data)
    {
        _jobSystem.EnqueueJob(() =>
        {
            // Process on worker thread
            var result = Process(data);

            // Return to main thread
            _jobSystem.EnqueueJobOnMainThread(() =>
            {
                OnComplete(result);
            });
        });
    }
}
```

### Service with Asset Loading

```csharp
public class MyService : IMyService
{
    private readonly IAssetManager _assetManager;
    private Texture2D? _texture;

    public void Initialize()
    {
        _texture = _assetManager.LoadTexture("path/to/texture.png");
    }

    public void Dispose()
    {
        _assetManager.UnloadAsset("path/to/texture.png");
    }
}
```

## Next Steps

- Check out existing plugins: `src/Lilly.Voxel.Plugin/` and `src/Lilly.Engine.GameObjects/`
- Read the [Architecture Guide](architecture.md) to understand system interactions
- Explore the [Lua Scripting Guide](lua-scripting.md) to expose your plugin to scripts
- Browse the [API Reference](../api/) for available interfaces and services

Happy plugin development!