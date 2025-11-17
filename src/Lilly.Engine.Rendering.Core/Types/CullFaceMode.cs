namespace Lilly.Engine.Rendering.Core.Types;

/// <summary>
/// Specifies which faces should be culled during rendering.
/// </summary>
public enum CullFaceMode : byte
{
    /// <summary>
    /// No face culling.
    /// </summary>
    None,

    /// <summary>
    /// Cull back-facing polygons (default).
    /// </summary>
    Back,

    /// <summary>
    /// Cull front-facing polygons.
    /// </summary>
    Front,

    /// <summary>
    /// Cull both front and back-facing polygons.
    /// </summary>
    FrontAndBack
}
