using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Rendering.Core.Context;

namespace Lilly.Engine.Modules;

[ScriptModule("window", "Provides functions to interact with the application window.")]
public class WindowModule
{
    private readonly RenderContext _renderContext;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;

    public WindowModule(RenderContext renderContext, IMainThreadDispatcher mainThreadDispatcher)
    {
        _renderContext = renderContext;
        _mainThreadDispatcher = mainThreadDispatcher;
    }

    /// <summary>
    /// Gets the current title of the application window.
    /// </summary>
    /// <returns>The current window title.</returns>
    [ScriptFunction("get_title", "Gets the title of the application window.")]
    public string GetTitle()
        => _renderContext.Window.Title;

    [ScriptFunction("set_refresh_rate", "Sets the refresh rate of the application window.")]
    public void SetRefreshRate(int refreshRate)
    {
        _mainThreadDispatcher.EnqueueAction(
            () =>
            {
                _renderContext.Window.FramesPerSecond = refreshRate;
                _renderContext.Window.UpdatesPerSecond = refreshRate;
            }
        );
    }

    [ScriptFunction("set_title", "Sets the title of the application window.")]
    public void SetTitle(string title)
    {
        _mainThreadDispatcher.EnqueueAction(
            () =>
            {
                _renderContext.Window.Title = title;
            }
        );
    }

    [ScriptFunction("set_vsync", "Enables or disables vertical synchronization (VSync) for the application window.")]
    public void SetVSync(bool vSync)
    {
        _mainThreadDispatcher.EnqueueAction(
            () =>
            {
                _renderContext.Window.VSync = vSync;
            }
        );
    }
}
