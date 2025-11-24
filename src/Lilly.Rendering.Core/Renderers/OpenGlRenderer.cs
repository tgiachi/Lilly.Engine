using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Data.Config;
using Lilly.Rendering.Core.Interfaces.Renderers;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Lilly.Rendering.Core.Renderers;

public class OpenGlRenderer : ILillyRenderer
{
    private readonly ILogger _logger = Log.ForContext<OpenGlRenderer>();
    private readonly WindowOptions _windowOptions;
    private readonly RenderContext _renderContext = new();
    private readonly GameTime _gameTime = new();

    public void Run()
    {

        _logger.Information("Starting render loop");
        _renderContext.Window.Run();

        _renderContext.Window.Dispose();
    }
    public event ILillyRenderer.RenderDelegate? OnRender;
    public event ILillyRenderer.UpdateDelegate? OnUpdate;
    public event ILillyRenderer.ResizeDelegate? OnResize;
    public event ILillyRenderer.ReadyDelegate? OnReady;
    public event ILillyRenderer.ClosingDelegate? OnClosing;


    public OpenGlRenderer(RenderConfig config)
    {

        _windowOptions = WindowOptions.Default;

        _windowOptions.Size = new(config.WindowConfig.Width, config.WindowConfig.Height);
        _windowOptions.Title = config.WindowConfig.Title;
        _windowOptions.FramesPerSecond = config.WindowConfig.MaxFramerate;
        _windowOptions.VSync = config.WindowConfig.VSync;
        _windowOptions.UpdatesPerSecond = config.WindowConfig.MaxFramerate;

        _windowOptions.API = new(ContextAPI.OpenGL, new(config.OpenGlApiLevel.Major, config.OpenGlApiLevel.Minor));
        _renderContext.Window = Window.Create(_windowOptions);

        _renderContext.Window.Update += WindowOnUpdate;
        _renderContext.Window.Render += WindowOnRender;

        _renderContext.Window.Load += WindowOnLoad;

        _renderContext.Window.FramebufferResize += WindowOnFramebufferResize;

        _renderContext.Window.Closing += WindowOnClosing;
    }

    private void WindowOnClosing()
    {
        _logger.Information("Window is closing");
        OnClosing?.Invoke();
    }

    private void WindowOnLoad()
    {
        _logger.Information(
            "Window loaded with API version: {API}",
            _windowOptions.API.Version.MajorVersion + "." + _windowOptions.API.Version.MinorVersion
        );
        _renderContext.Input = _renderContext.Window.CreateInput();
        _renderContext.OpenGl = GL.GetApi(_renderContext.Window);
        _renderContext.GraphicsDevice = new(_renderContext.OpenGl);

        OnReady?.Invoke(_renderContext);
    }

    private void WindowOnRender(double obj)
    {
        _gameTime.Update(obj);
        OnRender?.Invoke(_gameTime);
    }

    private void WindowOnUpdate(double obj)
    {
        _gameTime.Update(obj);
        OnUpdate?.Invoke(_gameTime);
    }

    private void WindowOnFramebufferResize(Vector2D<int> obj)
    {
        _renderContext.OpenGl.Viewport(0, 0, (uint)obj.X, (uint)obj.Y);
        _logger.Information("Window resized to {Width}x{Height}", obj.X, obj.Y);
        OnResize?.Invoke(obj.X, obj.Y);
    }
}
