using Lilly.Engine.Core.Extensions.Strings;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Scenes;
using Lilly.Engine.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.Scenes.Base;

public abstract class BaseScene : IScene
{
    public string Name { get; }
    public event IScene.SceneActivatedHandler? SceneActivated;

    private readonly IGameObjectManager _gameObjectManager;

    private readonly List<IGameObject> _gameObjects = [];

    protected BaseScene(IGameObjectManager gameObjectManager, string name)
    {
        _gameObjectManager = gameObjectManager;
        Name = name.ToSnakeCase();
    }

    protected void AddGameObject(IGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        _gameObjects.Add(gameObject);
        _gameObjectManager.AddGameObject(gameObject);
    }

    public virtual void Load()
    {
        SceneActivated?.Invoke(this);
    }

    protected void RemoveGameObject(IGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        _gameObjects.Remove(gameObject);
        _gameObjectManager.RemoveGameObject(gameObject);
    }

    public void Unload()
    {
        foreach (var gameObject in _gameObjects)
        {
            _gameObjectManager.RemoveGameObject(gameObject);
        }
        _gameObjects.Clear();
    }
}
