using System.Runtime.InteropServices;
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

    private readonly List<RenderSystemRegistration> _renderSystemsRegistrations;
    private readonly ILogger _logger = Log.ForContext<GraphicRenderPipeline>();

    // Command collection buffer - reused every frame to avoid allocations
    private readonly List<RenderCommand> _collectedCommands = new(2048);

    // Temporary buffer for filtered commands per layer (reused to avoid allocations)
    private List<RenderCommand> _filteredCommandsBuffer = new(1024);

    /// <summary>
    /// Gets the diagnostic information for the render pipeline.
    /// </summary>
    public RenderPipelineDiagnostics Diagnostics { get; } = new();

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
        Diagnostics.BeginFrame();

        foreach (var layer in _renderLayers.GetLayersSpan())
        {
            var layerCommands = layer.CollectRenderCommands(gameTime);

            // Record diagnostics for this layer
            Diagnostics.RecordLayerCommands(layer.Name, layerCommands.Count);

            // Collect all commands into a single buffer
            _collectedCommands.AddRange(layerCommands);
        }

        // This phase can be extended to:
        // - Sort by texture/shader to reduce GPU state changes
        // - Batch similar commands together
        // - Perform frustum culling
        // - Remove redundant state changes
        OptimizeCommands(_collectedCommands);

        // Commands are now processed in optimal order regardless of layer origin
        SubmitCommands(_collectedCommands);

        _collectedCommands.Clear();

        Diagnostics.EndFrame(gameTime.ElapsedGameTime);
    }

    /// <summary>
    /// Optimizes the collected render commands.
    /// Base implementation groups commands by type to minimize GPU state changes.
    /// Override in derived classes for more advanced optimization (texture sorting, depth sorting, etc.).
    /// </summary>
    /// <param name="commands">The list of commands to optimize.</param>
    protected virtual void OptimizeCommands(List<RenderCommand> commands)
    {
        if (commands.Count == 0)
            return;

        // BASIC OPTIMIZATION: Sort by CommandType to group similar operations
        // This alone provides significant performance improvements by:
        // 1. Grouping all Clear commands together
        // 2. Grouping all DrawTexture commands together (reduces texture binding)
        // 3. Grouping all DrawText commands together (reduces font atlas binding)
        // 4. Grouping all Window commands together
        // 5. Grouping all ImGui commands together
        //
        // For more advanced optimizations (texture/shader sorting, depth sorting):
        // - Create a custom GraphicRenderPipeline subclass in your game project
        // - Override this method with access to your specific payload types
        // - Implement sorting by texture handle, depth, font, etc.

        commands.Sort((x, y) => x.CommandType.CompareTo(y.CommandType));

    }

    /// <summary>
    /// Submits the optimized commands to the appropriate render systems.
    /// Filters commands by layer capabilities with zero allocations using buffer reuse.
    /// </summary>
    /// <param name="commands">The list of commands to submit.</param>
    protected virtual void SubmitCommands(List<RenderCommand> commands)
    {
        // Get a zero-copy span view of the commands list
        var commandsSpan = CollectionsMarshal.AsSpan(commands);

        // Process commands through each layer, filtering by supported types
        foreach (var layer in _renderLayers.GetLayersSpan())
        {
            // Reuse buffer instead of allocating new list
            _filteredCommandsBuffer.Clear();

            // Filter commands that this layer can process (in-place, no LINQ allocation)
            var supportedTypes = layer.SupportedCommandTypes;
            foreach (var cmd in commandsSpan)
            {
                if (supportedTypes.Contains(cmd.CommandType))
                {
                    _filteredCommandsBuffer.Add(cmd);
                }
            }

            // Only process if we have commands for this layer
            if (_filteredCommandsBuffer.Count > 0)
            {
                layer.ProcessRenderCommands(ref _filteredCommandsBuffer);
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

    public void EnqueueRenderCommand(RenderCommand command)
    {
        _collectedCommands.Add(command);
    }
}
