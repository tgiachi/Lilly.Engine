using DryIoc;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Data.Internal;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;
using Serilog;

namespace Lilly.Engine.Rendering.Core.Services;

public class GraphicRenderPipeline : IGraphicRenderPipeline
{
    private readonly RenderLayerCollection _renderLayers = new();
    private readonly IContainer _container;

    private readonly List<RenderSystemRegistration> _renderSystemsRegistrations;
    private readonly ILogger _logger = Log.ForContext<GraphicRenderPipeline>();

    public GraphicRenderPipeline(List<RenderSystemRegistration> renderSystemsRegistrations, IContainer container)
    {
        _renderSystemsRegistrations = renderSystemsRegistrations;
        _container = container;
    }

    public ReadOnlySpan<IRenderLayerSystem> RenderLayers => _renderLayers.GetLayersSpan();

    public void AddGameObject<TGameObject>(TGameObject gameObject) where TGameObject : IGameObject
    {
        _renderLayers.AddGameObject(gameObject);
    }

    public TRenderSystem? GetRenderLayerSystem<TRenderSystem>() where TRenderSystem : class, IRenderLayerSystem
        => _renderLayers.GetLayer<TRenderSystem>();

    public void Initialize()
    {
        foreach (var layer in _renderSystemsRegistrations)
        {
            var system = _container.Resolve<IRenderLayerSystem>(layer.Type);

            _logger.Information("Initializing Render Layer: {LayerName} type: {Layer}", system.Name, system.Layer);
            system.Initialize();
            _renderLayers.Add(system);
        }
    }

    public void RemoveGameObject<TGameObject>(TGameObject gameObject) where TGameObject : IGameObject
    {
        _renderLayers.RemoveGameObject(gameObject);
    }

    public void Render(GameTime gameTime)
    {
        foreach (var layer in _renderLayers.GetLayersSpan())
        {
            var collectRenderCommands = layer.CollectRenderCommands(gameTime);
            layer.ProcessRenderCommands(ref collectRenderCommands);
        }
    }

    public void Update(GameTime gameTime)
    {
        _renderLayers.UpdateAll(gameTime);
    }

    public void ViewportResize(int width, int height)
    {
        foreach (var layer in _renderLayers.GetLayersSpan())
        {
            layer.OnViewportResize(width, height);
        }
    }
}
