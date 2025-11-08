using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Data.Config;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;
using Lilly.Engine.Rendering.Core.Types;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using TrippyGL;

namespace Lilly.Engine.Renderers;

public class OpenGlRenderer : IGraphicRenderer
{
    private readonly ILogger _logger = Log.ForContext<OpenGlRenderer>();

    public string Name => "OpenGL";

    public RendererType RendererType => RendererType.OpenGL;

    public RenderContext Context { get; } = new();

    public event IGraphicRenderer.UpdateHandler? Update;

    public event IGraphicRenderer.RenderHandler? Render;
    public event IGraphicRenderer.ResizeHandler? Resize;

    public void Initialize(InitialEngineOptions options)
    {
        _logger.Information("Initializing Lilly Engine...");

        var windowOptions = WindowOptions.Default;
        windowOptions.Title = options.WindowTitle;
        windowOptions.API = new GraphicsAPI(
            ContextAPI.OpenGL,
            ContextProfile.Core,
            ContextFlags.Debug,
            new APIVersion(options.TargetRenderVersion.Major, options.TargetRenderVersion.Minor)
        );

        _logger.Information(
            "Initializing with OpenGL Version {Major}.{Minor}",
            options.TargetRenderVersion.Major,
            options.TargetRenderVersion.Minor
        );

        Context.Window = Window.Create(windowOptions);

        Context.Window.Load += WindowOnLoad;
        Context.Window.FramebufferResize += WindowOnFramebufferResize;
        Context.Window.Render += WindowOnRender;
        Context.Window.Update += WindowOnUpdate;
    }

    private void WindowOnLoad()
    {
        Context.Gl = Context.Window.CreateOpenGL();
        Context.GraphicsDevice = new GraphicsDevice(Context.Gl);
        Context.InputContext = Context.Window.CreateInput();
        Context.GraphicsDevice.ClearColor = Color4b.CornflowerBlue;

        _logger.Information("Window Loaded");
    }

    private void WindowOnFramebufferResize(Vector2D<int> size)
    {
        _logger.Information("Window Resized to {Width}x{Height}", size.X, size.Y);
        Context.GraphicsDevice.SetViewport(0, 0, (uint)size.X, (uint)size.Y);
        Resize?.Invoke(size.X, size.Y);
    }

    private void WindowOnUpdate(double obj)
    {
        Context.GameTime.Update(obj);
        Update?.Invoke(Context.GameTime);
    }

    private void WindowOnRender(double obj)
    {
        Context.GraphicsDevice.Clear(ClearBuffers.Color);
        Render?.Invoke(Context.GameTime);
    }

    public void Run()
    {
        Context.Window.Run();
    }
}
