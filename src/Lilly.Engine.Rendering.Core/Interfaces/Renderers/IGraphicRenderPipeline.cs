using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Data.Diagnostics;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.Rendering.Core.Interfaces.Renderers;

/// <summary>
/// Defines the contract for the graphics rendering pipeline.
/// </summary>
public interface IGraphicRenderPipeline : IGameObjectManager
{
    /// <summary>
    /// Gets the collection of render layers in the pipeline.
    /// </summary>
    ReadOnlySpan<IRenderLayerSystem> RenderLayers { get; }

    /// <summary>
    /// Gets the diagnostic information for the render pipeline.
    /// </summary>
    RenderPipelineDiagnostics Diagnostics { get; }



    /// <summary>
    /// Gets a specific render layer system by type.
    /// </summary>
    /// <typeparam name="TRenderSystem">The type of render system to retrieve.</typeparam>
    /// <returns>The render system instance, or null if not found.</returns>
    TRenderSystem? GetRenderLayerSystem<TRenderSystem>() where TRenderSystem : class, IRenderLayerSystem;

    /// <summary>
    /// Initializes the render pipeline and all registered render systems.
    /// </summary>
    void Initialize();


    /// <summary>
    /// Renders all layers for the current frame.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    void Render(GameTime gameTime);

    /// <summary>
    /// Updates all render layers.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    void Update(GameTime gameTime);

    /// <summary>
    /// Notifies all render layers of a viewport resize.
    /// </summary>
    /// <param name="width">The new viewport width.</param>
    /// <param name="height">The new viewport height.</param>
    void ViewportResize(int width, int height);

    void EnqueueRenderCommand(RenderCommand command);
}
