using Lilly.Engine.GameObjects.ThreeD.Lights;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Lights;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Lights;
using Lilly.Rendering.Core.Types;
using Serilog;

namespace Lilly.Engine.Services;

/// <summary>
/// Stores and clamps active lights for rendering and tracks the main shadow-casting light.
/// </summary>
public sealed class LightManager : ILightManager
{
    public int MaxDirectionalLights => 4;
    public int MaxPointLights => 32;
    public int MaxSpotLights => 16;

    private readonly IGameObjectManager _gameObjectManager;

    private readonly ILogger _logger = Log.ForContext<LightManager>();

    public DirectionalLight? ShadowLight { get; set; }

    public IReadOnlyList<DirectionalLight> DirectionalLights => _directionalLights;
    public IReadOnlyList<PointLight> PointLights => _pointLights;
    public IReadOnlyList<SpotLight> SpotLights => _spotLights;

    private readonly List<DirectionalLight> _directionalLights = [];
    private readonly List<PointLight> _pointLights = [];
    private readonly List<SpotLight> _spotLights = [];


    public LightManager(IGameObjectManager gameObjectManager)
    {
        _gameObjectManager = gameObjectManager;
        _gameObjectManager.GameObjectAdded += OnGameObjectAdded;
        _gameObjectManager.GameObjectRemoved += OnGameObjectRemoved;
    }

    private void OnGameObjectRemoved(IGameObject gameObject)
    {

    }

    private void OnGameObjectAdded(IGameObject gameObject)
    {
        _logger.Debug("Checking game object {GameObjectName} for lights", gameObject.Name);

        var added = false;
        LightType lightType = LightType.Point; // Default value to satisfy definite assignment

        if (gameObject is DirectionalLightGameObject directionalLightGameObject)
        {
            Add(directionalLightGameObject.Light);
            added = true;
            lightType = LightType.Directional;
        }

        if (gameObject is PointLightGameObject pointLightGameObject)
        {
            Add(pointLightGameObject.Light);
            added = true;
            lightType = LightType.Point;

        }

        if (gameObject is SpotLightGameObject spotLightGameObject)
        {
            Add(spotLightGameObject.Light);
            added = true;
            lightType = LightType.Spot;
        }

        if (added)
        {
            _logger.Information("Added {LightType} light from game object {GameObjectName}", lightType, gameObject.Name);
        }


    }

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
