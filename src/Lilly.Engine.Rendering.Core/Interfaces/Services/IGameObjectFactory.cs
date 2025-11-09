using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Interfaces.Services;

public interface IGameObjectFactory
{
    TGameObject CreateGameObject<TGameObject>() where TGameObject : class, IGameObject;

    IGameObject CreateGameObject(Type type);
}
