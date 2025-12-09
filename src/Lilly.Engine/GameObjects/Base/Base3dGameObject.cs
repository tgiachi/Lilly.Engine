using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Collections;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Primitives;
using TrippyGL;

namespace Lilly.Engine.GameObjects.Base;

public abstract class Base3dGameObject : IGameObject3d, IUpdateble, IInitializable
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public uint ZIndex { get; set; }

    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;

            foreach (var child in Children)
            {
                child.IsActive = value;
            }
        }
    }

    public IGameObject? Parent { get; set; }
    public IEnumerable<IGameObject> Children { get; } = new GameObjectCollection<IGameObject>();

    public bool IgnoreFrustumCulling { get; set; }

    private Transform3D _transform = new();

    public Transform3D Transform
    {
        get => _transform;
        set
        {
            if (value == null)
            {
                return;
            }

            if (_transform == value)
            {
                return;
            }

            _transform.Changed -= HandleTransformChanged;
            _transform = value;
            _transform.Changed += HandleTransformChanged;
            HandleTransformChanged(_transform);
        }
    }

    public Vector3 Position
    {
        get => Transform.Position;
        set => Transform.Position = value;
    }

    public Quaternion Rotation
    {
        get => Transform.Rotation;
        set => Transform.Rotation = value;
    }

    public Vector3 Scale
    {
        get => Transform.Scale;
        set => Transform.Scale = value;
    }

    public virtual BoundingBox BoundingBox
    {
        get
        {
            var center = Transform.Position;
            var half = Vector3.Abs(Transform.Scale) * 0.5f;

            Span<Vector3> offsets =
            [
                new(-half.X, -half.Y, -half.Z),
                new(-half.X, -half.Y, half.Z),
                new(-half.X, half.Y, -half.Z),
                new(-half.X, half.Y, half.Z),
                new(half.X, -half.Y, -half.Z),
                new(half.X, -half.Y, half.Z),
                new(half.X, half.Y, -half.Z),
                new(half.X, half.Y, half.Z)
            ];

            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);

            foreach (var offset in offsets)
            {
                var world = Vector3.Transform(offset, Transform.Rotation) + center;
                min = Vector3.Min(min, world);
                max = Vector3.Max(max, world);
            }

            return new(min, max);
        }
    }

    private readonly IGameObjectManager _gameObjectManager;

    public event Action? TransformChanged;

    protected Base3dGameObject(string name, IGameObjectManager gameObjectManager, uint zIndex = 0)
    {
        Name = name;
        _gameObjectManager = gameObjectManager;
        ZIndex = zIndex;
        IsActive = true;
        _transform.Changed += HandleTransformChanged;
    }

    public virtual void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera) { }

    public virtual void Initialize() { }

    public void OnRemoved()
    {
        foreach (var child in Children)
        {
            _gameObjectManager.RemoveGameObject(child);
        }
    }

    /// <summary>
    /// Updates the game object. Override to implement custom update logic.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public virtual void Update(GameTime gameTime)
    {
        if (!IsActive) { }
    }

    /// <summary>
    /// Adds a 2D game object as a child.
    /// </summary>
    /// <param name="gameObject">The game object to add.</param>
    protected void AddGameObject(params IGameObject[] gameObjects)
    {
        ArgumentNullException.ThrowIfNull(gameObjects);

        foreach (var gameObject in gameObjects)
        {
            if (Children is GameObjectCollection<IGameObject> collection)
            {
                collection.Add(gameObject);
                gameObject.Parent = this;

                _gameObjectManager.AddGameObject(gameObject);
            }
        }
    }

    protected TGameObject CreateGameObject<TGameObject>() where TGameObject : class, IGameObject
    {
        var gameObject = _gameObjectManager.CreateGameObject<TGameObject>();

        AddGameObject(gameObject);

        return gameObject;
    }

    protected TGameObject? GetGameObject<TGameObject>() where TGameObject : class, IGameObject
    {
        if (Children is GameObjectCollection<IGameObject> collection)
        {
            foreach (var child in collection)
            {
                if (child is TGameObject typedChild)
                {
                    return typedChild;
                }
            }
        }

        return _gameObjectManager.GetGameObjectOfType<TGameObject>();
    }

    /// <summary>
    /// Removes a 2D game object from the children collection.
    /// </summary>
    /// <param name="gameObject">The game object to remove.</param>
    protected void RemoveGameObject(IGameObject3d gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (Children is GameObjectCollection<IGameObject> collection)
        {
            if (collection.Remove(gameObject))
            {
                gameObject.Parent = null;
                _gameObjectManager.RemoveGameObject(gameObject);
            }
        }
    }

    private void HandleTransformChanged(Transform3D obj)
    {
        TransformChanged?.Invoke();
    }
}
