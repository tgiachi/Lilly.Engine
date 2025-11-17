using DryIoc;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Data.Config;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;

namespace Lilly.Engine.Interfaces.Bootstrap;

/// <summary>
/// Defines the interface for bootstrapping and managing the Lilly Engine lifecycle.
/// </summary>
public interface ILillyBootstrap
{
    /// <summary>
    /// Delegate for handling render operations during the render phase.
    /// </summary>
    /// <param name="gameTime">The current game time information.</param>
    delegate void RenderHandler(GameTime gameTime);

    /// <summary>
    ///  Delegate for configuring the dependency injection container.
    /// </summary>
    delegate void ConfiguringHandler(IContainer container);

    /// <summary>
    /// Delegate for handling update operations during the update phase.
    /// </summary>
    /// <param name="gameTime">The current game time information.</param>
    delegate void UpdateHandler(GameTime gameTime);

    /// <summary>
    /// Gets the graphics renderer instance.
    /// </summary>
    IGraphicRenderer Renderer { get; }

    /// <summary>
    /// Event raised during the render phase of each frame.
    /// </summary>
    event RenderHandler OnRender;

    /// <summary>
    /// Event raised during the update phase of each frame.
    /// </summary>
    event UpdateHandler OnUpdate;

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
