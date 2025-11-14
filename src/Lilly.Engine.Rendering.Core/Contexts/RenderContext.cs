using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Contexts;

/// <summary>
/// Contains the rendering context with references to window, graphics API, and input.
/// </summary>
public class RenderContext
{
    /// <summary>
    /// Gets or sets the window instance.
    /// </summary>
    public IWindow Window { get; set; }

    /// <summary>
    /// Gets or sets the OpenGL API instance.
    /// </summary>
    public GL Gl { get; set; }

    /// <summary>
    /// Gets or sets the graphics device for rendering operations.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; set; }

    /// <summary>
    /// Gets or sets the current game time information.
    /// </summary>
    public GameTime GameTime { get; set; }

    /// <summary>
    /// Gets or sets the input context for handling keyboard, mouse, and other input devices.
    /// </summary>
    public IInputContext InputContext { get; set; }

    /// <summary>
    /// Gets or sets the graphic renderer in use.
    /// </summary>
    public IGraphicRenderer Renderer { get; set; }
}
