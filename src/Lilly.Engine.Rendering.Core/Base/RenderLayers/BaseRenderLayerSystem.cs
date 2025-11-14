using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Base.RenderLayers;

/// <summary>
/// Base class for render layer systems that manage collections of game objects and handle rendering commands.
/// </summary>
public abstract class BaseRenderLayerSystem<TEntity> : IRenderLayerSystem where TEntity : IGameObject
{
    public RenderLayer Layer { get; }
    public string Name { get; }

    /// <summary>
    /// Gets the set of command types that this layer system can process.
    /// Override this in derived classes to specify which command types the layer handles.
    /// </summary>
    public virtual IReadOnlySet<RenderCommandType> SupportedCommandTypes { get; } =
        new HashSet<RenderCommandType>();

    private readonly Lock _collectionLock = new();

    protected List<RenderCommand> RenderCommands { get; } = new(512);

    protected GameObjectCollection<TEntity> GameObjects { get; } = new();

    protected BaseRenderLayerSystem(string name, RenderLayer layer)
    {
        Layer = layer;
        Name = name;
    }

    /// <summary>
    /// Adds a game object to the layer if it matches the entity type.
    /// </summary>
    /// <param name="gameObject">The game object to add.</param>
    public void Add(IGameObject gameObject)
    {
        lock (_collectionLock)
        {
            if (gameObject is TEntity entity)
            {
                GameObjects.Add(entity);

                if (gameObject is IInitializable initializable)
                {
                    initializable.Initialize();
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot add game object of type {gameObject.GetType().Name} to layer system of type {typeof(TEntity).Name}"
                );
            }
        }
    }

    /// <summary>
    /// Determines whether the specified game object can be added or removed from this layer.
    /// </summary>
    /// <param name="gameObject">The game object to check.</param>
    /// <returns>True if the game object can be added or removed, false otherwise.</returns>
    public virtual bool CanAddOrRemove(IGameObject gameObject)
        => gameObject is TEntity;

    /// <summary>
    /// Determines whether the specified game object can be removed from this layer.
    /// </summary>
    /// <param name="gameObject">The game object to check.</param>
    /// <returns>True if the game object can be removed, false otherwise.</returns>
    public bool CanRemove(IGameObject gameObject)
        => gameObject is TEntity;

    /// <summary>
    /// Removes all game objects from the layer.
    /// </summary>
    public void Clear()
    {
        lock (_collectionLock)
        {
            GameObjects.Clear();
        }
    }

    /// <summary>
    /// Collects render commands from all game objects in this layer.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <returns>A list of render commands to be processed.</returns>
    public virtual List<RenderCommand> CollectRenderCommands(GameTime gameTime)
    {
        RenderCommands.Clear();

        foreach (var gameObject in GetAllTypedGameObjects())
        {
            // Collect commands from each game object (including its children)
            foreach (var command in gameObject.Render(gameTime))
            {
                RenderCommands.Add(command);
            }
        }

        return RenderCommands;
    }

    /// <summary>
    /// Gets an enumerable collection of all game objects in the layer.
    /// </summary>
    /// <returns>An enumerable of game objects.</returns>
    public IEnumerable<IGameObject> GetAllGameObjects()
    {
        foreach (var gameObject in GameObjects)
        {
            yield return gameObject;
        }
    }

    /// <summary>
    /// Gets an enumerable collection of all game objects in the layer as the specific entity type.
    /// </summary>
    /// <returns>An enumerable of typed game objects.</returns>
    public IEnumerable<TEntity> GetAllTypedGameObjects()
    {
        foreach (var gameObject in GameObjects)
        {
            yield return gameObject;
        }
    }

    /// <summary>
    /// Initializes the render layer system.
    /// </summary>
    public virtual void Initialize() { }

    /// <summary>
    /// Handles viewport resize events.
    /// </summary>
    /// <param name="width">The new viewport width.</param>
    /// <param name="height">The new viewport height.</param>
    public virtual void OnViewportResize(int width, int height) { }

    /// <summary>
    /// Processes the provided render commands.
    /// </summary>
    /// <param name="renderCommands">The list of render commands to process.</param>
    public virtual void ProcessRenderCommands(ref List<RenderCommand> renderCommands) { }

    /// <summary>
    /// Removes a game object from the layer if it matches the entity type.
    /// </summary>
    /// <param name="gameObject">The game object to remove.</param>
    public void Remove(IGameObject gameObject)
    {
        lock (_collectionLock)
        {
            if (gameObject is TEntity entity)
            {
                GameObjects.Remove(entity);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot remove game object of type {gameObject.GetType().Name} from layer system of type {typeof(TEntity).Name}"
                );
            }
        }
    }

    /// <summary>
    /// Updates the render layer system.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>
    /// Adds a render command to the collection of commands for this frame.
    /// </summary>
    /// <param name="command">The render command to add.</param>
    protected void AddRenderCommand(RenderCommand command)
    {
        RenderCommands.Add(command);
    }
}
