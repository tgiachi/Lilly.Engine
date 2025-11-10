using Lilly.Engine.Commands;
using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;

namespace Lilly.Engine.Modules;

[ScriptModule("window", "Provides functions to interact with the application window.")]
public class WindowModule
{


    private readonly IGraphicRenderPipeline _graphicRenderPipeline;
    private readonly RenderContext _renderContext;
    public WindowModule(IGraphicRenderPipeline graphicRenderPipeline, RenderContext renderContext)
    {
        _graphicRenderPipeline = graphicRenderPipeline;
        _renderContext = renderContext;
    }

    [ScriptFunction("set_title", "Sets the title of the application window.")]
    public void SetTitle(string title)
    {
        _graphicRenderPipeline.EnqueueRenderCommand(RenderCommandHelpers.SetWindowTitle(title));
    }

    [ScriptFunction("get_title", "Gets the title of the application window.")]
    public string GetTitle()
    {
        return _renderContext.Window.Title;
    }


}
