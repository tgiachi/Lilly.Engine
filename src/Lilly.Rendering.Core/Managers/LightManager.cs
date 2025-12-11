using Lilly.Rendering.Core.Interfaces.Lights;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Lights;

namespace Lilly.Rendering.Core.Managers;

/// <summary>
/// Stores and clamps active lights for rendering and tracks the main shadow-casting light.
/// </summary>
public sealed class LightManager : ILightManager
{
    public int MaxDirectionalLights => 4;
    public int MaxPointLights => 32;
    public int MaxSpotLights => 16;

    public DirectionalLight? ShadowLight { get; set; }

    public IReadOnlyList<DirectionalLight> DirectionalLights => _directionalLights;
    public IReadOnlyList<PointLight> PointLights => _pointLights;
    public IReadOnlyList<SpotLight> SpotLights => _spotLights;

    private readonly List<DirectionalLight> _directionalLights = new();
    private readonly List<PointLight> _pointLights = new();
    private readonly List<SpotLight> _spotLights = new();

    public void Add(ILight light)
    {
        ArgumentNullException.ThrowIfNull(light);

        switch (light)
        {
            case DirectionalLight directional when !_directionalLights.Contains(directional):
                _directionalLights.Add(directional);

                if (directional.CastsShadows && ShadowLight is null)
                {
                    ShadowLight = directional;
                }

                break;

            case PointLight point when !_pointLights.Contains(point):
                _pointLights.Add(point);

                break;

            case SpotLight spot when !_spotLights.Contains(spot):
                _spotLights.Add(spot);

                break;

            case DirectionalLight:
            case PointLight:
            case SpotLight:
                // Already present; nothing to do.
                break;

            default:
                throw new NotSupportedException($"Unsupported light type: {light.GetType().Name}");
        }
    }

    public bool Remove(ILight light)
    {
        ArgumentNullException.ThrowIfNull(light);

        var removed = light switch
        {
            DirectionalLight directional => _directionalLights.Remove(directional),
            PointLight point             => _pointLights.Remove(point),
            SpotLight spot               => _spotLights.Remove(spot),
            _                            => false
        };

        if (removed && ReferenceEquals(ShadowLight, light))
        {
            ShadowLight = null;
        }

        return removed;
    }

    public void Clear()
    {
        _directionalLights.Clear();
        _pointLights.Clear();
        _spotLights.Clear();
        ShadowLight = null;
    }

    public (DirectionalLight[] directional, PointLight[] points, SpotLight[] spots) GetActiveLights()
    {
        return (
                   CopyActive(_directionalLights, MaxDirectionalLights),
                   CopyActive(_pointLights, MaxPointLights),
                   CopyActive(_spotLights, MaxSpotLights)
               );
    }

    private static T[] CopyActive<T>(List<T> source, int maxCount)
        where T : ILight
    {
        var capacity = Math.Min(maxCount, source.Count);
        var buffer = new T[capacity];
        var idx = 0;

        for (var i = 0; i < source.Count && idx < capacity; i++)
        {
            var light = source[i];

            if (!light.IsActive)
            {
                continue;
            }

            buffer[idx++] = light;
        }

        if (idx == capacity)
        {
            return buffer;
        }

        var trimmed = new T[idx];
        Array.Copy(buffer, trimmed, idx);

        return trimmed;
    }
}
