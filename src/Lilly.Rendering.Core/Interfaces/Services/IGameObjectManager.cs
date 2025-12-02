using Lilly.Rendering.Core.Interfaces.Entities;

namespace Lilly.Rendering.Core.Interfaces.Services;

public interface IGameObjectManager
{
    void AddGameObject<TEntity>(TEntity entity) where TEntity : IGameObject;

    void CreateGameObject<TEntity>() where TEntity : class, IGameObject;

    void RemoveGameObject<TEntity>(TEntity entity) where TEntity : IGameObject;

    TGameObject? GetGameObject<TGameObject>() where TGameObject : IGameObject;
}
