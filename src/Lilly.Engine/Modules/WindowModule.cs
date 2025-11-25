using Lilly.Engine.Core.Attributes.Scripts;

using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.Modules;

[ScriptModule("window", "Provides functions to interact with the application window.")]
public class WindowModule
{
    private readonly IRenderPipeline _graphicRenderPipeline;
    private readonly RenderContext _renderContext;

    public WindowModule(IRenderPipeline graphicRenderPipeline, RenderContext renderContext)
    {
        _graphicRenderPipeline = graphicRenderPipeline;
        _renderContext = renderContext;
    }

    [ScriptFunction("get_title", "Gets the title of the application window.")]

    /// <summary>
    /// Gets the current title of the application window.
    /// </summary>
    /// <returns>The current window title.</returns>
    public string GetTitle()
        => _renderContext.Window.Title;
}
