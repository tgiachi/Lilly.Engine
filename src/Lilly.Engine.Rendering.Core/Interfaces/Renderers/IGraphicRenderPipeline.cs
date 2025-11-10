using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Data.Diagnostics;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Interfaces.Renderers;

/// <summary>
/// Defines the contract for the graphics rendering pipeline.
/// </summary>
public interface IGraphicRenderPipeline
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
    /// Adds a game object to the appropriate render layer.
    /// </summary>
    /// <typeparam name="TGameObject">The type of game object.</typeparam>
    /// <param name="gameObject">The game object to add.</param>
    void AddGameObject<TGameObject>(TGameObject gameObject) where TGameObject : IGameObject;

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
    /// Removes a game object from its render layer.
    /// </summary>
    /// <typeparam name="TGameObject">The type of game object.</typeparam>
    /// <param name="gameObject">The game object to remove.</param>
    void RemoveGameObject<TGameObject>(TGameObject gameObject) where TGameObject : IGameObject;

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
