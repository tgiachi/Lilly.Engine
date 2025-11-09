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

    public IGameObject Create()
    {
        return (IGameObject)_container.Resolve(_type);
    }

    public bool Return(IGameObject obj)
    {
        // Reset the game object state before returning to pool
        obj.Parent = null;
        obj.Children.Clear();

        return true;
    }
}
