# Introduction

Lilly.Engine is a game engine built with C# and .NET that focuses on clean architecture and modularity. If you've ever wanted to understand how game engines work under the hood, or you need a flexible foundation for your game projects, this might be what you're looking for.

## Why Another Game Engine?

There are plenty of game engines out there, from Unity and Unreal to Godot. So why build another one?

Lilly.Engine started as an exploration into game engine architecture - a way to understand what happens between your game code and the pixels on screen. Over time it evolved into something more complete: a modular engine where you can see and modify every part of the stack.

The goal isn't to compete with commercial engines. Instead, it offers:

- **Transparency** - Everything is open and readable. No black boxes.
- **Learning** - Great for understanding engine architecture and game systems.
- **Flexibility** - Take what you need, replace what you don't.
- **C# and .NET** - Modern language features, cross-platform support, and a rich ecosystem.

## Design Philosophy

### Modularity First

The engine is built around a plugin system. Core functionality lives in the base engine, but major features like voxel rendering or UI controls are separate plugins. This means:

- Games only include what they actually use
- You can swap out systems without touching core code
- Adding new features doesn't bloat existing projects

### Dependency Injection Everywhere

Every system uses dependency injection through DryIoc. This makes testing easier, reduces coupling, and lets you override behaviors without forking the codebase.

```csharp
// Need a custom asset manager? Just register it
container.Register<IAssetManager, MyAssetManager>(Reuse.Singleton);
```

### Scripting for Flexibility, C# for Performance

Lua scripts handle game logic, AI, and rapid prototyping. Performance-critical code (rendering, physics, mesh generation) stays in C#. This gives you flexibility where you need it and speed where it matters.

### Clean Separation of Layers

The engine follows a layered architecture:

1. **Core** - Interfaces, utilities, base services
2. **Rendering** - Graphics abstraction (OpenGL, but replaceable)
3. **Engine** - Scene management, input, audio, assets
4. **Plugins** - Optional features (voxels, UI, etc.)
5. **Game** - Your actual game code

Each layer only depends on layers below it. You can understand and modify one without touching the others.

## What Can You Build?

The engine includes systems for:

**2D Games**
- Sprite rendering with batching
- Orthographic cameras
- UI controls for menus and HUDs
- Texture atlases for sprite sheets

**3D Games**
- First-person and third-person cameras
- 3D model rendering
- Shader support
- 3D positional audio

**Voxel Worlds**
- Infinite terrain generation
- Custom block types
- Lighting system with propagation
- Chunk-based rendering with LOD
- Caves, erosion, and decoration

**Any Genre**
- Event-driven architecture for game logic
- Scene management with transitions
- Input handling (keyboard, mouse, gamepad)
- Job system for multi-threading
- Asset management for textures, audio, fonts

## Technical Overview

### Performance

The engine includes several systems for performance:

- **Job System** - Multi-threaded task execution with priority queues
- **Sprite Batching** - Reduces draw calls for 2D rendering
- **Chunk-based Rendering** - Only renders visible voxel chunks
- **Face Culling** - Voxel faces are culled when not visible

### Debugging

Built-in debuggers help you understand what's happening:

- **Job System Debugger** - See worker threads, queue sizes, and execution times
- **Render Pipeline Debugger** - Inspect layers and game object counts
- **Performance Debugger** - Frame times, FPS, and memory usage
- **Camera Debugger** - Position, rotation, and projection matrices

All debuggers use ImGui for visualization.

### Extensibility

Everything is designed to be extended:

- Implement `ILillyPlugin` to add new functionality
- Override service implementations through DI
- Create custom game objects by inheriting base classes
- Add Lua modules to expose new APIs to scripts
- Write custom shaders for rendering effects

## The Stack

Lilly.Engine is built on these technologies:

| Component | Library |
|-----------|---------|
| Language | C# 13, .NET 10 |
| DI Container | DryIoc |
| Graphics | TrippyGL (OpenGL) |
| Windowing | Silk.NET |
| Audio | OpenAL |
| Fonts | FontStashSharp |
| Scripting | MoonSharp (Lua) |
| CLI | ConsoleAppFramework |
| Logging | Serilog |

All dependencies are managed through NuGet, and the engine targets modern .NET for cross-platform support.

## Who Is This For?

**Students and Learners**
If you're learning game development or engine architecture, this codebase is designed to be readable. The modular structure makes it easy to focus on one system at a time.

**Indie Developers**
You get a working engine with real features (rendering, audio, input, scripting) that you can build on. The MIT license means you can use it in commercial projects.

**Engine Developers**
Want to understand how job systems, chunk rendering, or script integration works? The code is here and documented.

**Modders and Tinkerers**
Everything is configurable through code, Lua scripts, or JSON. You can tweak terrain generation, create new block types, or script entire game systems without touching C#.

## What This Engine Is Not

To set expectations:

- **Not Production-Ready** - This is an active personal project. APIs may change.
- **Not Optimized for Scale** - It works great for indie projects, but isn't optimized like commercial engines.
- **Not Feature-Complete** - No built-in physics, no visual editor, no asset pipeline (yet).
- **Not A Unity Replacement** - Different goals and scope.

If you need a proven engine for a commercial project, use Unity or Unreal. If you want to learn, experiment, or build something from a solid foundation, this engine might fit.

## Next Steps

Ready to get your hands dirty?

1. [Getting Started](getting-started.md) - Set up your development environment and build your first scene
2. [Architecture Guide](architecture.md) - Deep dive into how the engine is structured
3. [Plugin Development](plugin-development.md) - Learn how to extend the engine
4. [Lua Scripting](lua-scripting.md) - Script game logic without recompiling

Or jump straight into the [API Reference](../api/) to explore the codebase.