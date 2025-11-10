using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Base.RenderLayers;

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

    private List<RenderCommand> _renderCommands = new(512);

    protected List<RenderCommand> RenderCommands => _renderCommands;

    protected GameObjectCollection<TEntity> GameObjects { get; } = new();

    protected BaseRenderLayerSystem(string name, RenderLayer layer)
    {
        Layer = layer;
        Name = name;
    }

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

    public virtual bool CanAddOrRemove(IGameObject gameObject)
        => gameObject is TEntity;

    public bool CanRemove(IGameObject gameObject)
        => gameObject is TEntity;

    public void Clear()
    {
        lock (_collectionLock)
        {
            GameObjects.Clear();
        }
    }

    public virtual List<RenderCommand> CollectRenderCommands(GameTime gameTime)
    {
        _renderCommands.Clear();

        foreach (var gameObject in GetAllTypedGameObjects())
        {
            // Collect commands from each game object (including its children)
            foreach (var command in gameObject.Render(gameTime))
            {
                _renderCommands.Add(command);
            }
        }

        return _renderCommands;
    }

    public IEnumerable<IGameObject> GetAllGameObjects()
    {
        foreach (var gameObject in GameObjects)
        {
            yield return gameObject;
        }
    }

    public IEnumerable<TEntity> GetAllTypedGameObjects()
    {
        foreach (var gameObject in GameObjects)
        {
            yield return gameObject;
        }
    }

    public virtual void Initialize() { }

    public virtual void OnViewportResize(int width, int height) { }

    public virtual void ProcessRenderCommands(ref List<RenderCommand> renderCommands) { }

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

    public virtual void Update(GameTime gameTime) { }

    protected void AddRenderCommand(RenderCommand command)
    {
        _renderCommands.Add(command);
    }
}
