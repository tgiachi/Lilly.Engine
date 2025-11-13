using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Data.Config;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Interfaces.Renderers;

/// <summary>
/// Defines the contract for graphic renderers that handle window management and rendering operations.
/// </summary>
public interface IGraphicRenderer
{
    /// <summary>
    /// Delegate for handling render operations.
    /// </summary>
    /// <param name="gameTime">The current game time information.</param>
    delegate void RenderHandler(GameTime gameTime);

    /// <summary>
    /// Delegate for handling update operations.
    /// </summary>
    /// <param name="gameTime">The current game time information.</param>
    delegate void UpdateHandler(GameTime gameTime);

    /// <summary>
    /// Delegate for handling window resize events.
    /// </summary>
    /// <param name="width">The new window width.</param>
    /// <param name="height">The new window height.</param>
    delegate void ResizeHandler(int width, int height);

    /// <summary>
    /// Gets the name of the renderer.
    /// </summary>
    string Name { get; }


    /// <summary>
    ///  Gets or sets the target frames per second for rendering.
    /// </summary>
    double TargetFramesPerSecond { get; set; }

    /// <summary>
    /// Gets the type of renderer (OpenGL, DirectX, Vulkan, Metal).
    /// </summary>
    RendererType RendererType { get; }

    /// <summary>
    /// Gets the rendering context containing window, graphics device, and input information.
    /// </summary>
    RenderContext Context { get; }

    /// <summary>
    /// Event raised during the update phase of each frame.
    /// </summary>
    event UpdateHandler Update;

    /// <summary>
    /// Event raised during the render phase of each frame.
    /// </summary>
    event RenderHandler Render;

    /// <summary>
    /// Event raised when the window is resized.
    /// </summary>
    event ResizeHandler Resize;

    /// <summary>
    ///  Event raised after the rendering of each frame is completed.
    /// </summary>
    event RenderHandler PostRender;

    /// <summary>
    /// Initializes the renderer with the specified engine options.
    /// </summary>
    /// <param name="options">The engine initialization options.</param>
    void Initialize(InitialEngineOptions options);

    /// <summary>
    /// Starts the rendering loop.
    /// </summary>
    void Run();
}
