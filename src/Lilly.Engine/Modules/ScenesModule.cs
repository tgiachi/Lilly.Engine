using DryIoc;
using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Scenes.Transitions;
using Lilly.Engine.Wrappers.Scenes;
using MoonSharp.Interpreter;
using Serilog;
using TrippyGL;

namespace Lilly.Engine.Modules;

[ScriptModule("scenes", "Provides functionality related to scene management.")]
public class ScenesModule
{
    private readonly IContainer _container;

    private readonly RenderContext _renderContext;
    private readonly ISceneManager _sceneManager;

    public ScenesModule(ISceneManager sceneManager, RenderContext renderContext, IContainer container)
    {
        _sceneManager = sceneManager;
        _renderContext = renderContext;
        _container = container;
    }

    [ScriptFunction("add_scene", "Adds a new scene to the scene manager.")]
    public void AddScene(string name, Closure loadFunction)
    {
        var scene = new LuaSceneWrap(_container.Resolve<IGameObjectManager>(), name, loadFunction);

        _sceneManager.RegisterScene(scene);
    }

    [ScriptFunction("load_scene", "Loads a scene by its name.")]
    public void LoadScene(string name)
    {
        var scene = _sceneManager.GetScene(name);

        if (scene == null)
        {
            Log.Error("Scene with name {SceneName} not found.", name);

            return;
        }
        var viewport = _renderContext.GraphicsDevice.Viewport;

        _sceneManager.ChangeScene(scene, new FadeTransition((int)viewport.Width, (int)viewport.Height, Color4b.Black, 1.0f));
    }
}
