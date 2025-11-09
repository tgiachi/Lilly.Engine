namespace Lilly.Engine.Rendering.Core.Data.Config;

/// <summary>
/// Configuration options for engine initialization.
/// </summary>
public class InitialEngineOptions
{
    /// <summary>
    /// Gets or sets the window title displayed in the title bar.
    /// </summary>
    public string WindowTitle { get; set; } = "Squid Engine";

    /// <summary>
    /// Gets or sets the graphics configuration options.
    /// </summary>
    public InitialGraphicOptions GraphicOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the target graphics API version to use.
    /// </summary>
    public GraphicApiVersion TargetRenderVersion { get; set; } = new(3, 3, 0, 0);
}
