using DryIoc;
using Lilly.Engine.Data.Config;
using Lilly.Rendering.Core.Interfaces.Renderers;

namespace Lilly.Engine.Interfaces.Bootstrap;

public interface ILillyBootstrap
{
    /// <summary>
    ///  Delegate for configuring the dependency injection container.
    /// </summary>
    delegate void ConfiguringHandler(IContainer container);

    /// <summary>
    /// Gets the graphics renderer instance.
    /// </summary>
    IGraphicRenderer Renderer { get; }

    /// <summary>
    /// Event raised during the render phase of each frame.
    /// </summary>
    event IGraphicRenderer.RenderDelegate OnRender;

    /// <summary>
    /// Event raised during the update phase of each frame.
    /// </summary>
    event IGraphicRenderer.UpdateDelegate OnUpdate;

    /// <summary>
    ///  Event raised when configuring the dependency injection container.
    /// </summary>
    event ConfiguringHandler OnConfiguring;

    /// <summary>
    /// Initializes the engine with the specified configuration options.
    /// </summary>
    /// <param name="options">The engine initialization options.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    Task InitializeAsync(InitialEngineOptions options);

    /// <summary>
    /// Starts the engine's main loop.
    /// </summary>
    /// <returns>A task representing the asynchronous run operation.</returns>
    Task RunAsync();

    /// <summary>
    /// Shuts down the engine and releases all resources.
    /// </summary>
    /// <returns>A task representing the asynchronous shutdown operation.</returns>
    Task ShutdownAsync();
}
