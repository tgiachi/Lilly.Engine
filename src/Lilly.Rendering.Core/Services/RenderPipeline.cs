using DryIoc;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Data.Internal;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Pipeline;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Types;
using Serilog;

namespace Lilly.Rendering.Core.Services;

public class RenderPipeline : IRenderPipeline
{
    private readonly IContainer _container;
    private readonly List<RenderLayerRegistration> _renderLayerRegistrations;
    private readonly ILogger _logger = Log.ForContext<RenderPipeline>();
    private readonly Lock _renderLayersLock = new();
    private readonly Dictionary<RenderPriority, List<IRenderLayer>> _renderLayers = new();

    private readonly IGameObjectFactory _gameObjectFactory;

    public IEnumerable<IRenderLayer> RenderLayers
    {
        get
        {
            lock (_renderLayersLock)
            {
                return _renderLayers.Values.SelectMany(layerList => layerList).ToList();
            }
        }
    }

    public RenderPipeline(
        IContainer container,
        List<RenderLayerRegistration> renderLayerRegistrations,
        RenderContext renderContext,
        IGameObjectFactory gameObjectFactory
    )

    {
        _container = container;
        _renderLayerRegistrations = renderLayerRegistrations;
        _gameObjectFactory = gameObjectFactory;

        CreateLayers();
        AddRegisteredRenderLayer();

        renderContext.Renderer.OnUpdate += Update;
        renderContext.Renderer.OnRender += Render;
    }

    private void CreateLayers()
    {
        foreach (var value in Enum.GetValues<RenderPriority>())
        {
            lock (_renderLayersLock)
            {
                _renderLayers[value] = [];
            }
        }
    }

    private void AddRegisteredRenderLayer()
    {
        foreach (var renderLayerRegistration in _renderLayerRegistrations)
        {
            var renderLayer = (IRenderLayer)_container.Resolve(renderLayerRegistration.LayerType);

            AddRenderLayer(renderLayer);
        }
    }

    public void AddRenderLayer<TRenderLayer>(TRenderLayer renderLayer) where TRenderLayer : IRenderLayer
    {
        lock (_renderLayersLock)
        {
            _logger.Information(
                "Adding render layer {RenderLayer} with priority {Priority}",
                renderLayer.Name,
                renderLayer.Priority
            );

            if (!_renderLayers.TryGetValue(renderLayer.Priority, out List<IRenderLayer>? value))
            {
                value = [];
                _renderLayers[renderLayer.Priority] = value;
            }

            value.Add(renderLayer);

            renderLayer.IsActive = true;
            renderLayer.Initialize();
        }
    }

    public void Update(GameTime gameTime)
    {
        lock (_renderLayersLock)
        {
            foreach (var renderLayerList in _renderLayers.Values)
            {
                foreach (var renderLayer in renderLayerList)
                {
                    if (renderLayer.IsActive)
                    {
                        renderLayer.Update(gameTime);
                    }
                }
            }
        }
    }

    public void Render(GameTime gameTime)
    {
        lock (_renderLayersLock)
        {
            foreach (var renderLayerList in _renderLayers.Values)
            {
                foreach (var renderLayer in renderLayerList)
                {
                    if (renderLayer.IsActive)
                    {
                        renderLayer.Render(gameTime);
                    }
                }
            }
        }
    }

    public void AddGameObject<TEntity>(TEntity entity) where TEntity : IGameObject
    {
        var addedToLayer = new List<string>();

        lock (_renderLayersLock)
        {
            foreach (var renderLayerList in _renderLayers.Values)
            {
                foreach (var renderLayer in renderLayerList)
                {
                    if (renderLayer.CanAdd(entity))
                    {
                        renderLayer.AddEntity(entity);
                        addedToLayer.Add(renderLayer.Name);
                    }
                }
            }
        }

        if (addedToLayer.Count == 0)
        {
            _logger.Warning("Entity of type {EntityType} was not added to any render layer", typeof(TEntity).Name);
        }
        else
        {
            if (entity is IInitializable initializable)
            {
                _logger.Debug("Initializing entity {EntityType}", entity.Name);
                initializable.Initialize();
            }

            _logger.Information(
                "Entity of type {EntityType} (Parent: {Parent}) was added to render layers: {RenderLayers}",
                entity.Name,
                entity.Parent?.Name ?? "None",
                string.Join(", ", addedToLayer)
            );
        }
    }

    public TEntity CreateGameObject<TEntity>() where TEntity : class, IGameObject
    {
        var entity = _gameObjectFactory.Create<TEntity>();

        AddGameObject(entity);

        return entity;
    }

    public void RemoveGameObject<TEntity>(TEntity entity) where TEntity : IGameObject
    {
        lock (_renderLayersLock)
        {
            foreach (var renderLayerList in _renderLayers.Values)
            {
                foreach (var renderLayer in renderLayerList)
                {
                    if (renderLayer.CanAdd(entity))
                    {
                        renderLayer.RemoveEntity(entity);
                    }
                }

                entity.OnRemoved();
            }
        }
    }

    public TGameObject? GetGameObject<TGameObject>() where TGameObject : IGameObject
    {
        lock (_renderLayersLock)
        {
            foreach (var renderLayerList in _renderLayers.Values)
            {
                foreach (var renderLayer in renderLayerList)
                {
                    var entity = renderLayer.GetEntity<TGameObject>();
                    if (entity != null)
                    {
                        return entity;
                    }
                }
            }

            return default;
        }
    }
}
