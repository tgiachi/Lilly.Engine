namespace Lilly.Rendering.Core.Interfaces.Entities;

/// <summary>
/// Marker interface for objects that should receive shadows in the lighting pass.
/// </summary>
public interface IShadowReceiver3d
{
    /// <summary>
    /// Whether this object should sample the shadow map.
    /// </summary>
    bool ReceiveShadows { get; }
}
