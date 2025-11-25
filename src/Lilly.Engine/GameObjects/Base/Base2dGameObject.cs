using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Collections;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;
using Lilly.Rendering.Core.Primitives;

namespace Lilly.Engine.GameObjects.Base;

public abstract class Base2dGameObject : IGameObject2d, IUpdateble
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public uint ZIndex { get; set; }
    public bool IsActive { get; set; }

    public IGameObject? Parent { get; set; }
    public IEnumerable<IGameObject> Children { get; } = new GameObjectCollection<IGameObject2d>();

    protected ILillySpriteBatcher? SpriteBatcher { get; private set; }

    public Transform2D Transform { get; } = new Transform2D();

    protected Base2dGameObject(string name, uint zIndex = 0)
    {
        Name = name;
        ZIndex = zIndex;
        IsActive = true;
    }

    public void Draw(GameTime gameTime, ILillySpriteBatcher spriteBatcher)
    {
        if (!IsActive)
        {
            return;
        }

        SpriteBatcher = spriteBatcher;

        OnDraw(gameTime);

        foreach (var child in Children)
        {
            if (child is IGameObject2d child2d)
            {
                child2d.Draw(gameTime, spriteBatcher);
            }
        }
    }

    protected void AddGameObject2d(IGameObject2d gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (Children is GameObjectCollection<IGameObject2d> collection)
        {
            collection.Add(gameObject);
            gameObject.Parent = this;
        }
    }

    protected void RemoveGameObject2d(IGameObject2d gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (Children is GameObjectCollection<IGameObject2d> collection)
        {
            if (collection.Remove(gameObject))
            {
                gameObject.Parent = null;
            }
        }
    }

    protected virtual void OnDraw(GameTime gameTime) { }

    public virtual void Update(GameTime gameTime) { }
}
