using Lilly.Engine.Core.Extensions.Strings;
using Lilly.Engine.Interfaces.Scenes;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.Scenes.Base;

public abstract class BaseScene : IScene
{
    public string Name { get; }
    public event IScene.SceneActivatedHandler? SceneActivated;

    private readonly IRenderPipeline _renderPipeline;

    private readonly List<IGameObject> _gameObjects = [];

    protected BaseScene(IRenderPipeline renderPipeline, string name)
    {
        _renderPipeline = renderPipeline;
        Name = name.ToSnakeCase();
    }

    public virtual void Load()
    {
        SceneActivated?.Invoke(this);
    }

    public void Unload()
    {
        foreach (var gameObject in _gameObjects)
        {
            _renderPipeline.RemoveGameObject(gameObject);
        }
        _gameObjects.Clear();
    }

    protected void AddGameObject(IGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        _gameObjects.Add(gameObject);
        _renderPipeline.AddGameObject(gameObject);
    }

    protected void RemoveGameObject(IGameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        _gameObjects.Remove(gameObject);
        _renderPipeline.RemoveGameObject(gameObject);
    }
}
