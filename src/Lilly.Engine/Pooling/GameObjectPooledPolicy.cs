using DryIoc;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Microsoft.Extensions.ObjectPool;

namespace Lilly.Engine.Pooling;

public class GameObjectPooledPolicy : IPooledObjectPolicy<IGameObject>
{
    private readonly IContainer _container;
    private readonly Type _type;

    /// <summary>
    /// Initializes a new instance of the GameObjectPooledPolicy class.
    /// </summary>
    /// <param name="container">The dependency injection container.</param>
    /// <param name="type">The type of game object to create.</param>
    public GameObjectPooledPolicy(IContainer container, Type type)
    {
        _container = container;
        _type = type;
    }

    /// <summary>
    /// Creates a new game object instance for the pool.
    /// </summary>
    /// <returns>A new game object instance.</returns>
    public IGameObject Create()
    {
        return (IGameObject)_container.Resolve(_type);
    }

    /// <summary>
    /// Resets the game object state before returning to the pool.
    /// </summary>
    /// <param name="obj">The game object to reset.</param>
    /// <returns>True if the object can be returned to the pool.</returns>
    public bool Return(IGameObject obj)
    {
        // Reset basic hierarchy state
        obj.Parent = null;
        obj.Children.Clear();

        // If the object implements IPoolable, allow it to customize the reset behavior
        if (obj is IPoolable poolable)
        {
            poolable.ResetForPooling();
        }

        return true;
    }
}
