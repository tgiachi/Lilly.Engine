using System.Diagnostics;
using Lilly.Engine.Core.Utils;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Data.Config;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;
using Lilly.Engine.Rendering.Core.Types;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Lilly.Engine.Renderers;

/// <summary>
/// Implements an OpenGL-based graphics renderer for the Lilly Engine.
/// </summary>
public class OpenGlRenderer : IGraphicRenderer
{
    private readonly ILogger _logger = Log.ForContext<OpenGlRenderer>();

    /// <summary>
    /// Gets the name of the renderer.
    /// </summary>
    public string Name => "OpenGL";

    /// <summary>
    /// Gets or sets the target frames per second for rendering.
    /// </summary>
    public double TargetFramesPerSecond { get; set; } = 60.0;

    private double _targetDelta => 1.0 / TargetFramesPerSecond;

    /// <summary>
    /// Gets the type of renderer.
    /// </summary>
    public RendererType RendererType => RendererType.OpenGL;

    /// <summary>
    /// Gets the rendering context containing window, graphics device, and input information.
    /// </summary>
    public RenderContext Context { get; } = new();

    private DpiManager _dpiManager;

    /// <summary>
    /// Event raised during the update phase of each frame.
    /// </summary>
    public event IGraphicRenderer.UpdateHandler? Update;

    /// <summary>
    /// Event raised during the render phase of each frame.
    /// </summary>
    public event IGraphicRenderer.RenderHandler? Render;

    /// <summary>
    /// Event raised when the window is resized.
    /// </summary>
    public event IGraphicRenderer.ResizeHandler? Resize;

    public event IGraphicRenderer.RenderHandler? PostRender;

    /// <summary>
    /// Initializes the OpenGL renderer with the specified engine options.
    /// </summary>
    /// <param name="options">The engine initialization options.</param>
    public void Initialize(InitialEngineOptions options)
    {
        _logger.Information("Initializing Lilly Engine...");

        var windowOptions = WindowOptions.Default;
        windowOptions.Title = options.WindowTitle;
        windowOptions.Samples = 4;
        windowOptions.API = new(
            ContextAPI.OpenGL,
            ContextProfile.Core,
            ContextFlags.Debug,
            new(options.TargetRenderVersion.Major, options.TargetRenderVersion.Minor)
        );

        windowOptions.Size = options.GraphicOptions.WindowSize;
        windowOptions.VSync = options.GraphicOptions.VSync;

        _logger.Information(
            "Initializing with OpenGL Version {Major}.{Minor}",
            options.TargetRenderVersion.Major,
            options.TargetRenderVersion.Minor
        );

        _logger.Information("OS: {OS}", PlatformUtils.GetCurrentPlatform());


        Context.Window = Window.Create(windowOptions);

        Context.Window.Load += WindowOnLoad;
        Context.Window.FramebufferResize += WindowOnFramebufferResize;
        Context.Window.Render += WindowOnRender;
        Context.Window.Update += WindowOnUpdate;
        Context.Window.ShouldSwapAutomatically = true;

        Context.Renderer = this;
    }

    /// <summary>
    /// Starts the rendering loop.
    /// </summary>
    public void Run()
    {
        Context.Window.Run();
    }

    /// <summary>
    /// Handles the window framebuffer resize event and updates the viewport.
    /// </summary>
    /// <param name="size">The new framebuffer size.</param>
    private void WindowOnFramebufferResize(Vector2D<int> size)
    {
        _logger.Information("Window Resized to {Width}x{Height}", size.X, size.Y);

        Resize?.Invoke(size.X, size.Y);
    }

    /// <summary>
    /// Handles the window load event and initializes OpenGL context and graphics device.
    /// </summary>
    private void WindowOnLoad()
    {
        Context.Gl = Context.Window.CreateOpenGL();
        Context.GraphicsDevice = new(Context.Gl);
        Context.InputContext = Context.Window.CreateInput();

        _dpiManager = new(Context.Window, Context.Gl, Context.GraphicsDevice);
        _logger.Information("Vendor: {Vendor}", Context.GraphicsDevice.GLVendor);
        _logger.Information("Renderer: {Vendor}", Context.GraphicsDevice.GLRenderer);
        _logger.Information("Window Loaded");
    }

    /// <summary>
    /// Handles the window render event, clears the screen, and invokes the Render event.
    /// </summary>
    /// <param name="obj">The elapsed time since the last render.</param>
    private void WindowOnRender(double obj)
    {
        var start = Stopwatch.GetTimestamp();

        Context.GameTime = Context.GameTime.Update(obj);
        _dpiManager.Initialize();
        Render?.Invoke(Context.GameTime);
        PostRender?.Invoke(Context.GameTime);

        var targetTicks = (long)(Stopwatch.Frequency * _targetDelta);
        var remainingTicks = targetTicks - (Stopwatch.GetTimestamp() - start);

        if (remainingTicks > 0)
        {
            var sleepMs = (int)(remainingTicks * 1000 / Stopwatch.Frequency) - 1;

            if (sleepMs > 0)
            {
                Thread.Sleep(sleepMs);
            }

            while (Stopwatch.GetTimestamp() - start < targetTicks)
            {
                Thread.SpinWait(10);
            }
        }
    }

    /// <summary>
    /// Handles the window update event and invokes the Update event.
    /// </summary>
    /// <param name="obj">The elapsed time since the last update.</param>
    private void WindowOnUpdate(double obj)
    {
        Context.GameTime = Context.GameTime.Update(obj);
        Update?.Invoke(Context.GameTime);
    }
}
