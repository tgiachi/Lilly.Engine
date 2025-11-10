using System.Runtime.InteropServices;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Types;
using Serilog;

namespace Lilly.Engine.Rendering.Core.Collections;

/// <summary>
/// High-performance collection for managing render layer systems.
/// Optimized for fast iteration (every frame) with minimal allocations.
/// Supports multiple layers with the same priority.
/// </summary>
public sealed class RenderLayerCollection
{
    private readonly ILogger _logger = Log.ForContext<RenderLayerCollection>();
    private readonly List<IRenderLayerSystem> _layers;
    private readonly Dictionary<RenderLayer, List<IRenderLayerSystem>> _layersByEnum;
    private bool _isDirty;

    /// <summary>
    /// Gets the number of layers in the collection.
    /// </summary>
    public int Count => _layers.Count;

    /// <summary>
    /// Initializes a new instance of the RenderLayerCollection class.
    /// </summary>
    public RenderLayerCollection()
    {
        _layers = new(Enum.GetValues<RenderLayer>().Length);
        _layersByEnum = new(Enum.GetValues<RenderLayer>().Length);
        _isDirty = false;
    }

    /// <summary>
    /// Initializes a new instance of the RenderLayerCollection class with a specified capacity.
    /// </summary>
    /// <param name="capacity">Initial capacity for the collection.</param>
    public RenderLayerCollection(int capacity)
    {
        _layers = new(capacity);
        _layersByEnum = new(capacity);
        _isDirty = false;
    }

    /// <summary>
    /// Adds a render layer system to the collection.
    /// Multiple layers with the same priority are allowed.
    /// </summary>
    /// <param name="layer">The layer system to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when layer is null.</exception>
    public void Add(IRenderLayerSystem layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        _layers.Add(layer);

        if (!_layersByEnum.TryGetValue(layer.Layer, out var layerList))
        {
            layerList = [];
            _layersByEnum[layer.Layer] = layerList;
        }

        layerList.Add(layer);
        _isDirty = true;
    }

    /// <summary>
    /// Adds a game object to all layers that can accept it.
    /// </summary>
    /// <param name="gameObject">The game object to add.</param>
    /// <summary>
    /// Adds a game object to all layers that can accept it.
    /// </summary>
    /// <param name="gameObject">The game object to add.</param>
    public void AddGameObject(IGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);
        EnsureSorted();

        foreach (var layer in _layers)
        {
            if (layer.CanAddOrRemove(gameObject))
            {
                _logger.Debug(
                    "Adding GameObject of type {GameObjectType} to Layer {LayerName} ({LayerEnum})",
                    gameObject.GetType().Name,
                    layer.Name,
                    layer.Layer
                );

                layer.Add(gameObject);
            }
        }
    }

    /// <summary>
    /// Clears all layers from the collection.
    /// </summary>
    public void Clear()
    {
        _layers.Clear();
        _layersByEnum.Clear();
        _isDirty = false;
    }

    /// <summary>
    /// Checks if at least one layer with the specified RenderLayer enum exists.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum to check.</param>
    /// <returns>True if at least one layer exists, false otherwise.</returns>
    /// <summary>
    /// Checks if at least one layer with the specified RenderLayer enum exists.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum to check.</param>
    /// <returns>True if at least one layer exists, false otherwise.</returns>
    public bool Contains(RenderLayer layerEnum)
        => _layersByEnum.ContainsKey(layerEnum) && _layersByEnum[layerEnum].Count > 0;

    /// <summary>
    /// Executes an action for each layer in priority order.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <summary>
    /// Executes an action for each layer in priority order.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void ForEach(Action<IRenderLayerSystem> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsureSorted();

        foreach (var layer in _layers)
        {
            action(layer);
        }
    }

    /// <summary>
    /// Gets an enumerator for iterating over all layers.
    /// Layers are automatically sorted before enumeration.
    /// </summary>
    /// <returns>An enumerator.</returns>
    /// <summary>
    /// Gets an enumerator for iterating over all layers.
    /// Layers are automatically sorted before enumeration.
    /// </summary>
    /// <returns>An enumerator.</returns>
    public List<IRenderLayerSystem>.Enumerator GetEnumerator()
    {
        EnsureSorted();

        return _layers.GetEnumerator();
    }

    /// <summary>
    /// Gets the first render layer with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum.</param>
    /// <returns>The first layer if found, otherwise null.</returns>
    /// <summary>
    /// Gets the first render layer with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum.</param>
    /// <returns>The first layer if found, otherwise null.</returns>
    public IRenderLayerSystem? GetLayer(RenderLayer layerEnum)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layerList) && layerList.Count > 0)
        {
            return layerList[0];
        }

        return null;
    }

    /// <summary>
    /// Gets the first render layer of the specified type.
    /// </summary>
    /// <typeparam name="TLayer">The type of the layer to get.</typeparam>
    /// <returns>The first layer of the specified type if found, otherwise null.</returns>
    /// <summary>
    /// Gets the first render layer of the specified type.
    /// </summary>
    /// <typeparam name="TLayer">The type of the layer to get.</typeparam>
    /// <returns>The first layer of the specified type if found, otherwise null.</returns>
    public TLayer? GetLayer<TLayer>() where TLayer : class, IRenderLayerSystem
    {
        foreach (var layer in _layers)
        {
            if (layer is TLayer tLayer)
            {
                return tLayer;
            }
        }

        return default;
    }

    /// <summary>
    /// Gets all render layers with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum.</param>
    /// <returns>List of layers with the specified priority, or empty list if none found.</returns>
    /// <summary>
    /// Gets all render layers with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum.</param>
    /// <returns>List of layers with the specified priority, or empty list if none found.</returns>
    public IReadOnlyList<IRenderLayerSystem> GetLayers(RenderLayer layerEnum)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layerList))
        {
            return layerList.AsReadOnly();
        }

        return [];
    }

    /// <summary>
    /// Gets a read-only span of all layers sorted by priority.
    /// Use this for custom iteration scenarios.
    /// </summary>
    /// <returns>A read-only span of layers.</returns>
    /// <summary>
    /// Gets a read-only span of all layers sorted by priority.
    /// Use this for custom iteration scenarios.
    /// </summary>
    /// <returns>A read-only span of layers.</returns>
    public ReadOnlySpan<IRenderLayerSystem> GetLayersSpan()
    {
        EnsureSorted();

        return CollectionsMarshal.AsSpan(_layers);
    }

    /// <summary>
    /// Removes all render layers with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum of the layers to remove.</param>
    /// <returns>True if at least one layer was removed, false if not found.</returns>
    /// <summary>
    /// Removes all render layers with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum of the layers to remove.</param>
    /// <returns>True if at least one layer was removed, false if not found.</returns>
    public bool Remove(RenderLayer layerEnum)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layerList))
        {
            foreach (var layer in layerList)
            {
                _layers.Remove(layer);
            }
            _layersByEnum.Remove(layerEnum);
            _isDirty = true;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes a specific render layer from the collection.
    /// </summary>
    /// <param name="layer">The layer to remove.</param>
    /// <returns>True if the layer was removed, false if not found.</returns>
    /// <summary>
    /// Removes a specific render layer from the collection.
    /// </summary>
    /// <param name="layer">The layer to remove.</param>
    /// <returns>True if the layer was removed, false if not found.</returns>
    public bool Remove(IRenderLayerSystem layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (_layers.Remove(layer))
        {
            if (_layersByEnum.TryGetValue(layer.Layer, out var layerList))
            {
                layerList.Remove(layer);

                if (layerList.Count == 0)
                {
                    _layersByEnum.Remove(layer.Layer);
                }
            }
            _isDirty = true;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes a game object from all layers.
    /// </summary>
    /// <param name="gameObject">The game object to remove.</param>
    /// <summary>
    /// Removes a game object from all layers.
    /// </summary>
    /// <param name="gameObject">The game object to remove.</param>
    public void RemoveGameObject(IGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);
        EnsureSorted();

        foreach (var layer in _layers)
        {
            if (layer.CanAddOrRemove(gameObject))
            {
                layer.Remove(gameObject);
            }
        }
    }

    /// <summary>
    /// Tries to get the first render layer with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum.</param>
    /// <param name="layer">The first layer if found, otherwise null.</param>
    /// <returns>True if at least one layer was found, false otherwise.</returns>
    /// <summary>
    /// Tries to get the first render layer with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum.</param>
    /// <param name="layer">The first layer if found, otherwise null.</param>
    /// <returns>True if at least one layer was found, false otherwise.</returns>
    public bool TryGetLayer(RenderLayer layerEnum, out IRenderLayerSystem? layer)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layerList) && layerList.Count > 0)
        {
            layer = layerList[0];

            return true;
        }
        layer = null;

        return false;
    }

    /// <summary>
    /// Updates all layers in priority order.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    /// <summary>
    /// Updates all layers in priority order.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    public void UpdateAll(GameTime gameTime)
    {
        EnsureSorted();

        foreach (var layer in _layers)
        {
            layer.Update(gameTime);
        }
    }

    /// <summary>
    /// Ensures layers are sorted by their RenderLayer priority.
    /// Only sorts if the collection has been modified.
    /// </summary>
    private void EnsureSorted()
    {
        if (_isDirty)
        {
            _layers.Sort((a, b) => a.Layer.CompareTo(b.Layer));
            _isDirty = false;
        }
    }
}
