using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;

/// <summary>
/// Defines a system for managing a specific render layer, including adding/removing game objects, collecting and processing
/// render commands.
/// </summary>
public interface IRenderLayerSystem
{
    RenderLayer Layer { get; }

    string Name { get; }

    /// <summary>
    /// Gets the set of command types that this layer system can process.
    /// This enables efficient command filtering in the rendering pipeline.
    /// </summary>
    IReadOnlySet<RenderCommandType> SupportedCommandTypes { get; }

    /// <summary>
    /// Adds a game object to this render layer.
    /// </summary>
    /// <param name="gameObject">The game object to add.</param>
    void Add(IGameObject gameObject);

    /// <summary>
    /// Determines whether the specified game object can be added or removed from this layer.
    /// </summary>
    /// <param name="gameObject">The game object to check.</param>
    /// <returns>True if the game object can be added or removed, false otherwise.</returns>
    bool CanAddOrRemove(IGameObject gameObject);

    /// <summary>
    /// Removes all game objects from this render layer.
    /// </summary>
    void Clear();

    /// <summary>
    /// Collects render commands from all game objects in this layer.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <returns>A list of render commands to be processed.</returns>
    List<RenderCommand> CollectRenderCommands(GameTime gameTime);

    /// <summary>
    /// Gets an enumerable collection of all game objects in this layer.
    /// </summary>
    /// <returns>An enumerable of game objects.</returns>
    IEnumerable<IGameObject> GetAllGameObjects();

    /// <summary>
    /// Initializes the render layer system.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Handles viewport resize events.
    /// </summary>
    /// <param name="width">The new viewport width.</param>
    /// <param name="height">The new viewport height.</param>
    void OnViewportResize(int width, int height);

    /// <summary>
    /// Processes the provided render commands.
    /// </summary>
    /// <param name="renderCommands">The list of render commands to process.</param>
    void ProcessRenderCommands(ref List<RenderCommand> renderCommands);

    /// <summary>
    /// Removes a game object from this render layer.
    /// </summary>
    /// <param name="gameObject">The game object to remove.</param>
    void Remove(IGameObject gameObject);

    /// <summary>
    /// Updates the render layer system.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    void Update(GameTime gameTime);
}
