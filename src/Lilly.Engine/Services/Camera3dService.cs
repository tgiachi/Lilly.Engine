using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using TrippyGL;

namespace Lilly.Engine.Services;

/// <summary>
/// Service for managing 3D cameras in the engine.
/// Allows registration, unregistration, and activation of cameras.
/// </summary>
public class Camera3dService : ICamera3dService
{
    private readonly List<ICamera3D> _cameras = [];
    private readonly GraphicsDevice _graphicsDevice;

    public Camera3dService(RenderContext renderContext)
    {
        _graphicsDevice = renderContext.GraphicsDevice;
        renderContext.Renderer.Resize += UpdateViewport;
    }

    private void UpdateViewport(int width, int height)
    {
        UpdateViewport(new Viewport(0, 0, (uint)width, (uint)height));
    }

    //private Viewport _currentViewport;

    /// <summary>
    /// Gets or sets the currently active camera.
    /// Setting this to a registered camera makes it active.
    /// </summary>
    public ICamera3D? CurrentCamera
    {
        get => ActiveCamera;
        set
        {
            if (ActiveCamera == value)
            {
                return;
            }

            if (value != null && !_cameras.Contains(value))
            {
                throw new ArgumentException("Camera must be registered before being set as active.", nameof(value));
            }

            ActiveCamera = value;
            ActiveCameraChanged?.Invoke(ActiveCamera);
        }
    }

    public Viewport Viewport => _graphicsDevice.Viewport;

    /// <summary>
    /// Event raised whenever the active camera changes.
    /// </summary>
    public event Action<ICamera3D?>? ActiveCameraChanged;

    /// <summary>
    /// Gets the currently active camera (read-only).
    /// </summary>
    public ICamera3D? ActiveCamera { get; private set; }

    /// <summary>
    /// Gets all registered cameras (read-only).
    /// </summary>
    public IReadOnlyCollection<ICamera3D> Cameras => _cameras.AsReadOnly();

    /// <summary>
    /// Registers a camera with the service.
    /// </summary>
    /// <param name="camera">The camera to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when camera is null.</exception>
    /// <exception cref="ArgumentException">Thrown when camera is already registered.</exception>
    public void RegisterCamera(ICamera3D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        if (_cameras.Contains(camera))
        {
            throw new ArgumentException("Camera is already registered.", nameof(camera));
        }

        _cameras.Add(camera);

        // Update the viewport for the newly registered camera
        camera.AspectRatio = _graphicsDevice.Viewport.Width / (float)_graphicsDevice.Viewport.Height;
    }

    /// <summary>
    /// Removes a camera from the service.
    /// If the removed camera was active, ActiveCamera becomes null.
    /// </summary>
    /// <param name="camera">The camera to unregister.</param>
    /// <exception cref="ArgumentNullException">Thrown when camera is null.</exception>
    public void UnregisterCamera(ICamera3D camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        if (!_cameras.Remove(camera))
        {
            return;
        }

        // If the removed camera was active, set active camera to null
        if (ActiveCamera == camera)
        {
            CurrentCamera = null;
        }
    }

    /// <summary>
    /// Updates the cached viewport and notifies cameras when resolution changes.
    /// </summary>
    /// <param name="viewport">The new viewport dimensions.</param>
    public void UpdateViewport(Viewport viewport)
    {
        var aspectRatio = viewport.Width / (float)viewport.Height;

        // Update aspect ratio for all registered cameras
        foreach (var camera in _cameras)
        {
            camera.AspectRatio = aspectRatio;
        }
    }

    /// <summary>
    /// Updates all registered cameras.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public void Update(GameTime gameTime)
    {
        foreach (var camera in _cameras)
        {
            camera.Update(gameTime);
        }
    }
}
