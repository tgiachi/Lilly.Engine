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

    public IEnumerable<IRenderLayer> RenderLayers => _renderLayers.Values.SelectMany(layerList => layerList);

    public RenderPipeline(
        IContainer container,
        List<RenderLayerRegistration> renderLayerRegistrations,
        RenderContext renderContext
    )

    {
        _container = container;
        _renderLayerRegistrations = renderLayerRegistrations;
        AddRegisteredRenderLayer();

        renderContext.Renderer.OnUpdate += Update;
        renderContext.Renderer.OnRender += Render;
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
                    renderLayer.Update(gameTime);
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
                    renderLayer.Render(gameTime);
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
                    if (renderLayer.CanAdd<TEntity>())
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
            _logger.Information(
                "Entity of type {EntityType} was added to render layers: {RenderLayers}",
                typeof(TEntity).Name,
                string.Join(", ", addedToLayer)
            );
        }
    }

    public void RemoveGameObject<TEntity>(TEntity entity) where TEntity : IGameObject
    {
        lock (_renderLayersLock)
        {
            foreach (var renderLayerList in _renderLayers.Values)
            {
                foreach (var renderLayer in renderLayerList)
                {
                    if (renderLayer.CanAdd<TEntity>())
                    {
                        renderLayer.RemoveEntity(entity);
                    }
                }
            }
        }
    }
}
