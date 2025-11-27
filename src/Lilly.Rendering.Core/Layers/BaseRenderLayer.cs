using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Collections;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Pipeline;
using Lilly.Rendering.Core.Types;

namespace Lilly.Rendering.Core.Layers;

public class BaseRenderLayer<TGameObject> : IRenderLayer where TGameObject : class, IGameObject
{
    public string Name { get; }
    public RenderPriority Priority { get; }

    public int EntityCount => Entities.Count;

    protected GameObjectCollection<TGameObject> Entities { get; } = [];

    public BaseRenderLayer(string name, RenderPriority priority)
    {
        Name = name;
        Priority = priority;
    }

    public virtual void Initialize() { }
    public virtual void Update(GameTime gameTime) { }
    public virtual void Render(GameTime gameTime) { }

    public virtual bool CanAdd<TEntity>(TEntity entity)
    {
        return typeof(TGameObject).IsAssignableFrom(entity.GetType());
    }

    public virtual void AddEntity<TEntity>(TEntity entity) where TEntity : IGameObject
    {
        if (entity is TGameObject typedEntity)
        {
            Entities.Add(typedEntity);
        }
    }

    public void RemoveEntity<TEntity>(TEntity entity) where TEntity : IGameObject
    {
        if (entity is TGameObject typedEntity)
        {
            Entities.Remove(typedEntity);
        }
    }
}
