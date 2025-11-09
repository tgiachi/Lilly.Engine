namespace Lilly.Engine.Rendering.Core.Types;

/// <summary>
/// Defines the graphics API types supported by the engine.
/// </summary>
public enum RendererType
{
    /// <summary>
    /// OpenGL graphics API.
    /// </summary>
    OpenGL,

    /// <summary>
    /// DirectX graphics API (Windows).
    /// </summary>
    DirectX,

    /// <summary>
    /// Vulkan graphics API (cross-platform).
    /// </summary>
    Vulkan,

    /// <summary>
    /// Metal graphics API (Apple platforms).
    /// </summary>
    Metal
}
