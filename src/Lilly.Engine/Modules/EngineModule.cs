using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Rendering.Core.Context;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Modules;

[ScriptModule("engine", "Provides core engine functionalities.")]
public class EngineModule
{
    private readonly RenderContext _renderContext;

    public EngineModule(RenderContext renderContext)
    {
        _renderContext = renderContext;
    }

    [ScriptFunction("on_update", "Registers a closure to be called on each engine update cycle.")]
    public void OnUpdate(Closure update)
    {
        _renderContext.Renderer.OnUpdate += (gameTime) =>
                                            {
                                                update.Call(gameTime);
                                            };
    }
}
