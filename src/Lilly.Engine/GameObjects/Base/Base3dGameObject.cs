using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Collections;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Primitives;
using TrippyGL;

namespace Lilly.Engine.GameObjects.Base;

public abstract class Base3dGameObject : IGameObject3d
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public uint ZIndex { get; set; }

    public bool IsActive { get; set; }
    public IGameObject? Parent { get; set; }
    public IEnumerable<IGameObject> Children { get; } = new GameObjectCollection<IGameObject3d>();

    public bool IgnoreFrustumCulling { get; set; }

    public Transform3D Transform { get; set; } = new ();

    public virtual void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera) { }

    protected Base3dGameObject(string name, uint zIndex = 0)
    {
        Name = name;
        ZIndex = zIndex;
        IsActive = true;
    }

    /// <summary>
    /// Adds a 2D game object as a child.
    /// </summary>
    /// <param name="gameObject">The game object to add.</param>
    protected void AddGameObject(params IGameObject3d[] gameObjects)
    {
        ArgumentNullException.ThrowIfNull(gameObjects);

        foreach (var gameObject in gameObjects)
        {
            if (Children is GameObjectCollection<IGameObject3d> collection)
            {
                collection.Add(gameObject);
                gameObject.Parent = this;
            }
        }
    }

    /// <summary>
    /// Removes a 2D game object from the children collection.
    /// </summary>
    /// <param name="gameObject">The game object to remove.</param>
    protected void RemoveGameObject(IGameObject3d gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (Children is GameObjectCollection<IGameObject3d> collection)
        {
            if (collection.Remove(gameObject))
            {
                gameObject.Parent = null;
            }
        }
    }

    /// <summary>
    /// Updates the game object. Override to implement custom update logic.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public virtual void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }

        foreach (var child in Children)
        {
            if (child is IUpdateble child2d)
            {
                child2d.Update(gameTime);
            }
        }
    }

}
