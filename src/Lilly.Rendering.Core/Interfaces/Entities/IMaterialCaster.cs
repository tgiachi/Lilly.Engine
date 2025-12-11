using Lilly.Rendering.Core.Materials;

namespace Lilly.Rendering.Core.Interfaces.Entities;

/// <summary>
/// Exposes the material used by a renderable entity.
/// </summary>
public interface IMaterialCaster
{
    /// <summary>
    /// Gets the material assigned to this entity (null if none).
    /// </summary>
    Material? Material { get; }

    /// <summary>
    /// Indicates whether a material is available.
    /// </summary>
    bool HasMaterial => Material != null;
}
