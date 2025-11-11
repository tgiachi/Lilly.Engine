using System.Numerics;
using Serilog;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using TrippyGL;

namespace Lilly.Engine.Renderers;

public class DpiManager
{
    private readonly IWindow _window;
    private readonly GL _gl;
    private readonly GraphicsDevice _graphicsDevice;

    private readonly ILogger _logger = Log.ForContext<DpiManager>();


    private bool _isInitialized;

    public DpiManager(IWindow window, GL gl, GraphicsDevice graphicsDevice)
    {
        _window = window;
        _gl = gl;
        _graphicsDevice = graphicsDevice;

        UpdateDPIInfo();
        _window.FramebufferResize += WindowOnFramebufferResize;
    }

    public void UpdateSizes()
    {
        UpdateDPIInfo();
        _logger.Debug("DPI Updated: Window Size: {WindowSize}, Framebuffer Size: {FramebufferSize}, DPI Scale: {DPIScale}", WindowSize, FramebufferSize, DPIScale);

        _gl.Viewport(0, 0, (uint)FramebufferSize.X, (uint)FramebufferSize.Y);
        _graphicsDevice.SetViewport(0, 0, (uint)FramebufferSize.X, (uint)FramebufferSize.Y);
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        UpdateSizes();
        _isInitialized = true;
    }

    private void WindowOnFramebufferResize(Vector2D<int> obj)
    {
        UpdateSizes();

    }

    /// <summary>
    ///  Window Size
    /// </summary>
    public Vector2D<int> WindowSize { get; private set; }

    /// <summary>
    ///  Framebuffer Size
    /// </summary>
    public Vector2D<int> FramebufferSize { get; private set; }

    /// <summary>
    /// Dpi Scale
    /// </summary>
    public float DPIScale => (float)FramebufferSize.X / WindowSize.X;

    /// <summary>
    ///  Updates DPI information when the window is resized.
    /// </summary>
    private void UpdateDPIInfo()
    {
        WindowSize = _window.Size;
        FramebufferSize = _window.FramebufferSize;
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        float aspect = (float)FramebufferSize.X / FramebufferSize.Y;

        return Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4f,
            aspect,
            0.1f,
            100f
        );
    }

    public Vector2 ScaleMouseCoordinates(Vector2 mousePos)
    {
        return mousePos * DPIScale;
    }

    public float ScaleDimension(float logicalSize)
    {
        return logicalSize * DPIScale;
    }
}
