using Lilly.Rendering.Core.Interfaces.Lights;
using Lilly.Rendering.Core.Lights;

namespace Lilly.Rendering.Core.Interfaces.Services;

/// <summary>
/// Manages active lights and provides clamped snapshots for rendering.
/// </summary>
public interface ILightManager
{
    int MaxDirectionalLights { get; }
    int MaxPointLights { get; }
    int MaxSpotLights { get; }

    /// <summary>
    /// Optional main directional light used for shadow casting.
    /// </summary>
    DirectionalLight? ShadowLight { get; set; }

    IReadOnlyList<DirectionalLight> DirectionalLights { get; }
    IReadOnlyList<PointLight> PointLights { get; }
    IReadOnlyList<SpotLight> SpotLights { get; }

    void Add(ILight light);
    bool Remove(ILight light);
    void Clear();

    /// <summary>
    /// Returns active lights clamped to shader limits.
    /// </summary>
    (DirectionalLight[] directional, PointLight[] points, SpotLight[] spots) GetActiveLights();
}
