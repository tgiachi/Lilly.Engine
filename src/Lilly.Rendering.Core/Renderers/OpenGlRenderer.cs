using Lilly.Rendering.Core.Data.Config;
using Lilly.Rendering.Core.Interfaces.Renderers;
using Serilog;

namespace Lilly.Rendering.Core.Renderers;

public class OpenGlRenderer : ILillyRenderer
{
    private readonly ILogger _logger = Log.ForContext<OpenGlRenderer>();
    public event ILillyRenderer.RenderDelegate? OnRender;
    public event ILillyRenderer.UpdateDelegate? OnUpdate;
    public event ILillyRenderer.ResizeDelegate? OnResize;
    public event ILillyRenderer.ReadyDelegate? OnReady;

    private readonly RenderConfig _config;
    public OpenGlRenderer(RenderConfig config)
    {
        _config = config;
    }


}
