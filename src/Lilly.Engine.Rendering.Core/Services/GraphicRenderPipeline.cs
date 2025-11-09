using DryIoc;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Data.Diagnostics;
using Lilly.Engine.Rendering.Core.Data.Internal;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;
using Serilog;

namespace Lilly.Engine.Rendering.Core.Services;

/// <summary>
/// Manages the rendering pipeline, coordinating render layers and diagnostic information.
/// </summary>
public class GraphicRenderPipeline : IGraphicRenderPipeline
{
    private readonly RenderLayerCollection _renderLayers = new();
    private readonly IContainer _container;
    private readonly RenderPipelineDiagnostics _diagnostics = new();

    private readonly List<RenderSystemRegistration> _renderSystemsRegistrations;
    private readonly ILogger _logger = Log.ForContext<GraphicRenderPipeline>();

    /// <summary>
    /// Gets the diagnostic information for the render pipeline.
    /// </summary>
    public RenderPipelineDiagnostics Diagnostics => _diagnostics;

    /// <summary>
    /// Initializes a new instance of the GraphicRenderPipeline class.
    /// </summary>
    /// <param name="renderSystemsRegistrations">The list of render systems to register.</param>
    /// <param name="container">The dependency injection container.</param>
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

    /// <summary>
    /// Renders all layers and collects diagnostic information.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public void Render(GameTime gameTime)
    {
        _diagnostics.BeginFrame();

        foreach (var layer in _renderLayers.GetLayersSpan())
        {
            var collectRenderCommands = layer.CollectRenderCommands(gameTime);

            // Record diagnostics for this layer
            _diagnostics.RecordLayerCommands(layer.Name, collectRenderCommands.Count);

            layer.ProcessRenderCommands(ref collectRenderCommands);
        }

        _diagnostics.EndFrame(gameTime.ElapsedGameTime);
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
