using Lilly.Rendering.Core.Data.Game;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Types;

namespace Lilly.Rendering.Core.Interfaces.Pipeline;

public interface IRenderLayer
{
    string Name { get; }

    RenderPriority Priority { get; }

    void Update(GameTime gameTime);

    void Render(GameTime gameTime);

    bool CanAdd<TEntity>();

    void AddEntity<TEntity>(TEntity entity) where TEntity : IGameObject;

    void RemoveEntity<TEntity>(TEntity entity) where TEntity : IGameObject;
}
