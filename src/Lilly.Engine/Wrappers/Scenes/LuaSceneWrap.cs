using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Scenes.Base;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Wrappers.Scenes;

public class LuaSceneWrap : BaseScene
{
    private readonly Closure _loadFunction;

    public LuaSceneWrap(IGameObjectManager gameObjectManager, string name, Closure loadFunction) : base(
        gameObjectManager,
        name
    )
        => _loadFunction = loadFunction;

    public override void Load()
    {
        _loadFunction.Call(this);
        base.Load();
    }
}
