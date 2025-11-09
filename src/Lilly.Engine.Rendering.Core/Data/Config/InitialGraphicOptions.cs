using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Data.Config;

/// <summary>
/// Configuration options for graphics initialization.
/// </summary>
public class InitialGraphicOptions
{
    /// <summary>
    /// Gets or sets whether vertical synchronization is enabled.
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    /// Gets or sets the initial window size in pixels.
    /// </summary>
    public Vector2D<int> WindowSize { get; set; } = new(800, 600);
}
