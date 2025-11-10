using DryIoc;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Microsoft.Extensions.ObjectPool;

namespace Lilly.Engine.Pooling;

public class GameObjectPooledPolicy : IPooledObjectPolicy<IGameObject>
{
    private readonly IContainer _container;
    private readonly Type _type;

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
        // Reset the game object state before returning to pool
        obj.Parent = null;
        obj.Children.Clear();

        return true;
    }
}
