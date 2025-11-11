using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using TrippyGL;

namespace Lilly.Engine.Interfaces.Services;

/// <summary>
/// Provides access to registered 3d cameras and manages the active camera.
/// </summary>
public interface ICamera3dService
{
    ICamera3D? CurrentCamera { get; set; }

    Viewport Viewport { get; }

    /// <summary>
    /// Event raised whenever the active camera changes.
    /// </summary>
    event Action<ICamera3D?>? ActiveCameraChanged;

    /// <summary>
    /// Gets the currently active camera.
    /// </summary>
    ICamera3D? ActiveCamera { get; }

    /// <summary>
    /// Gets all registered cameras.
    /// </summary>
    IReadOnlyCollection<ICamera3D> Cameras { get; }

    /// <summary>
    /// Registers a camera with the service.
    /// </summary>
    /// <param name="camera">The camera to register.</param>
    void RegisterCamera(ICamera3D camera);

    /// <summary>
    /// Removes a camera from the service. If the removed camera was active, ActiveCamera becomes null.
    /// </summary>
    /// <param name="camera">The camera to unregister.</param>
    void UnregisterCamera(ICamera3D camera);

    /// <summary>
    /// Updates the cached viewport and notifies cameras when resolution changes.
    /// </summary>
    /// <param name="viewport">The new viewport dimensions.</param>
    void UpdateViewport(Viewport viewport);

}
