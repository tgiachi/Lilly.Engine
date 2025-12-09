using Lilly.Engine.Scenes.Base;
using Lilly.Rendering.Core.Interfaces.Services;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Wrappers.Scenes;

public class LuaSceneWrap : BaseScene
{
    private readonly Closure _loadFunction;

    public LuaSceneWrap(IRenderPipeline renderPipeline, string name, Closure loadFunction) : base(renderPipeline, name)
        => _loadFunction = loadFunction;

    public override void Load()
    {
        _loadFunction.Call(this);
        base.Load();
    }
}
