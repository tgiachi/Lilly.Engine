using Lilly.Engine.Rendering.Core.Interfaces.Scenes;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Scenes.Base;

public class LuaScene : BaseScene
{
    private readonly Table _table;

    public LuaScene(Table table, IGameObjectManager gameObjectManager, string name) : base(gameObjectManager, name)
    {
        _table = table;
        SceneActivated += OnSceneActivated;
    }

    public override void Load()
    {
        _table.Get("load")?.Function?.Call(this);
        base.Load();
    }

    private void OnSceneActivated(IScene scene)
    {
        _table.Get("on_activated")?.Function?.Call(this);
    }
}
