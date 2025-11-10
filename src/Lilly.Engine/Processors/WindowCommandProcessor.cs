using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Commands;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Processors;

/// <summary>
/// Processes Window render commands for window management operations.
/// </summary>
public class WindowCommandProcessor : ICommandProcessor
{
    private readonly RenderContext _renderContext;

    public RenderCommandType CommandType => RenderCommandType.Window;

    public WindowCommandProcessor(RenderContext renderContext)
    {
        _renderContext = renderContext;
    }

    public void Process(RenderCommand command)
    {
        var payload = command.GetPayload<WindowPayload>();

        switch (payload.SubCommandType)
        {
            case WindowSubCommandType.SetTitle:
                if (payload.Data is string title)
                {
                    _renderContext.Window.Title = title;
                }
                break;

            case WindowSubCommandType.SetFullscreen:
                if (payload.Data is bool isFullscreen)
                {
                    // TODO: Implement fullscreen toggle
                    // _renderContext.Window.WindowState = isFullscreen ? WindowState.Fullscreen : WindowState.Normal;
                }
                break;

            case WindowSubCommandType.SetSize:
                if (payload.Data is (int width, int height))
                {
                    // TODO: Implement window resize
                    // _renderContext.Window.Size = new Vector2D<int>(width, height);
                }
                break;

            case WindowSubCommandType.SetVSync:
                if (payload.Data is bool vsync)
                {
                    // TODO: Implement VSync toggle
                    // _renderContext.Window.VSync = vsync;
                }
                break;
        }
    }
}
