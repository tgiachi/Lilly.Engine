namespace Lilly.Engine.Rendering.Core.Types;

/// <summary>
/// Defines the rendering layer categories for organizing drawable elements.
/// </summary>
public enum RenderLayer : byte
{
    /// <summary>
    /// Background layer for elements rendered behind everything else.
    /// </summary>
    Background,

    /// <summary>
    /// Default layer for standard game elements.
    /// </summary>
    Default,

    /// <summary>
    /// Foreground layer for elements rendered in front of default elements.
    /// </summary>
    Foreground,

    /// <summary>
    /// Three-dimensional rendering layer for 3D objects.
    /// </summary>
    ThreeDimension,

    /// <summary>
    /// User interface layer for UI elements.
    /// </summary>
    UI,

    /// <summary>
    /// Debug layer for development and debugging visualizations.
    /// </summary>
    Debug
}
