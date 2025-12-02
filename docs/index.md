# Lilly.Engine

Welcome to **Lilly.Engine**, a modern, modular game engine built in C# for the .NET platform. Whether you're a hobbyist developer, an indie game studio, or just curious about game development, Lilly.Engine provides a solid foundation for creating 2D and 3D games with a focus on simplicity, performance, and extensibility.

## What is Lilly.Engine?

Lilly.Engine started as a personal project to explore game engine architecture and has grown into a fully-featured engine capable of handling everything from simple 2D platformers to complex 3D voxel worlds. It's designed with modularity in mind, so you can pick and choose the components you need for your project.

The engine is built on top of proven technologies like .NET Core, DryIoc for dependency injection, and TrippyGL for graphics rendering. It supports cross-platform development and can run on Windows, Linux, and potentially other platforms supported by .NET.

## Key Features

### Modular Architecture

Lilly.Engine is built with a plugin-based architecture. The core engine provides essential services like rendering, input handling, and asset management, while optional plugins add specialized functionality:

- **Voxel Plugin**: Generate and render infinite voxel worlds with customizable block types, lighting, and terrain generation.
- **Lua Scripting**: Integrate Lua scripts for game logic, allowing for rapid prototyping and modding capabilities.
- **UI System**: A flexible UI framework with controls like buttons, panels, and custom layouts.

### Rendering and Graphics

- Hardware-accelerated rendering using OpenGL through TrippyGL.
- Support for 2D and 3D graphics, including sprite batching, shader programs, and camera systems.
- Built-in support for textures, fonts, and custom shaders.

### Scripting and Logic

- Lua integration for scripting game behavior, AI, and events.
- C# API for performance-critical code and engine extensions.
- Event-driven architecture for handling user input, game updates, and custom events.

### Asset Management

- Centralized asset loading from embedded resources or files.
- Support for various asset types: textures, shaders, audio, fonts, and more.
- Automatic asset caching and management.

### Audio and Input

- OpenAL-based audio system for 3D positional audio and sound effects.
- Comprehensive input handling for keyboard, mouse, and gamepads.
- Customizable input mappings and contexts.

### Performance and Debugging

- Job system for parallel processing and performance optimization.
- Built-in debuggers for monitoring performance, rendering, and system metrics.
- Logging and notification systems for development and runtime feedback.

## Architecture Overview

Lilly.Engine follows a clean architecture pattern with clear separation of concerns:

- **Core Layer** (`Lilly.Engine.Core`): Provides fundamental services, utilities, and interfaces that other components build upon.
- **Engine Layer** (`Lilly.Engine`): The main engine assembly with modules for rendering, audio, input, and scene management.
- **Rendering Layer** (`Lilly.Rendering.Core`): Handles graphics rendering, cameras, and display management.
- **Game Layer** (`Lilly.Engine.Game`): Entry point and game-specific logic.
- **Plugins**: Optional modules like voxel worlds and scripting that extend the engine's capabilities.

The engine uses dependency injection throughout, making it easy to swap implementations, mock services for testing, and extend functionality.

## Getting Started

Ready to dive in? Here's how to get started with Lilly.Engine:

1. **Clone the Repository**: Grab the source code from our Git repository.
2. **Build the Solution**: Use your favorite .NET IDE or the command line to build the project.
3. **Explore the Samples**: Check out the included examples and plugins to see the engine in action.
4. **Read the Docs**: This documentation site has guides, API references, and tutorials to help you along.

For more detailed instructions, see our [Getting Started](docs/getting-started.md) guide.

## Community and Support

Lilly.Engine is an open-source project, and we welcome contributions from the community. Whether it's bug reports, feature requests, or code contributions, feel free to get involved.

- **Issues and Discussions**: Use GitHub issues for bug reports and feature requests.
- **Documentation**: Help improve this documentation by submitting pull requests.
- **Discord/Server**: Join our community for discussions and support (link TBD).

## License

Lilly.Engine is released under the MIT License, making it free to use for personal and commercial projects.

---

_Built with ❤️ using C# and .NET_</content>
<parameter name="filePath">docs/index.md
