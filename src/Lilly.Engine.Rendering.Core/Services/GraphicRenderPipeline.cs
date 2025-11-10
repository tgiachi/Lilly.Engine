using DryIoc;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
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

    // Command collection buffer - reused every frame to avoid allocations
    private List<RenderCommand> _collectedCommands = new(2048);

    /// <summary>
    /// Gets the diagnostic information for the render pipeline.
    /// </summary>
    public RenderPipelineDiagnostics Diagnostics => _diagnostics;

    /// <summary>
    /// Initializes a new instance of the GraphicRenderPipeline class.
    /// </summary>
    /// <param name="renderSystemsRegistrations">The list of render systems to register.</param>
    /// <param name="container">The dependency injection container.</param>
    public GraphicRenderPipeline(
        List<RenderSystemRegistration> renderSystemsRegistrations,
        IContainer container)
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
    /// Uses a separated Collect/Submit architecture for better optimization opportunities.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public void Render(GameTime gameTime)
    {
        _diagnostics.BeginFrame();

        // PHASE 1: COLLECT - Gather all render commands from all layers
        _collectedCommands.Clear();

        foreach (var layer in _renderLayers.GetLayersSpan())
        {
            var layerCommands = layer.CollectRenderCommands(gameTime);

            // Record diagnostics for this layer
            _diagnostics.RecordLayerCommands(layer.Name, layerCommands.Count);

            // Collect all commands into a single buffer
            _collectedCommands.AddRange(layerCommands);
        }

        // PHASE 2: OPTIMIZE (optional) - Sort and optimize commands
        // This phase can be extended to:
        // - Sort by texture/shader to reduce GPU state changes
        // - Batch similar commands together
        // - Perform frustum culling
        // - Remove redundant state changes
        OptimizeCommands(_collectedCommands);

        // PHASE 3: SUBMIT - Process all optimized commands
        // Commands are now processed in optimal order regardless of layer origin
        SubmitCommands(_collectedCommands);

        _diagnostics.EndFrame(gameTime.ElapsedGameTime);
    }

    /// <summary>
    /// Optimizes the collected render commands.
    /// Override this method to implement custom optimization strategies.
    /// </summary>
    /// <param name="commands">The list of commands to optimize.</param>
    protected virtual void OptimizeCommands(List<RenderCommand> commands)
    {
        // Default implementation: no optimization
        // Subclasses can override to implement:
        // - Texture/shader sorting
        // - Command batching
        // - Culling
    }

    /// <summary>
    /// Submits the optimized commands to the appropriate render systems.
    /// Filters commands by layer capabilities for optimal performance.
    /// </summary>
    /// <param name="commands">The list of commands to submit.</param>
    protected virtual void SubmitCommands(List<RenderCommand> commands)
    {
        // Process commands through each layer, filtering by supported types
        foreach (var layer in _renderLayers.GetLayersSpan())
        {
            // Filter commands that this layer can process
            var filteredCommands = commands
                .Where(cmd => layer.SupportedCommandTypes.Contains(cmd.CommandType))
                .ToList();

            if (filteredCommands.Count > 0)
            {
                layer.ProcessRenderCommands(ref filteredCommands);
            }
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
