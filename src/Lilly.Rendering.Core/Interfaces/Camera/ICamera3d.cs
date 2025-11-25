using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Primitives;
using TrippyGL;

namespace Lilly.Rendering.Core.Interfaces.Camera;

/// <summary>
/// Represents a 3D camera with position, orientation, and projection parameters.
/// </summary>
public interface ICamera3D
{
    /// <summary>
    /// Gets or sets whether the camera is enabled.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the camera name.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the camera position in world space.
    /// </summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets the camera rotation as a quaternion.
    /// </summary>
    /// <remarks>
    /// Using quaternions avoids gimbal lock and provides smooth interpolation.
    /// </remarks>
    Quaternion Rotation { get; set; }

    /// <summary>
    /// Gets or sets the target point the camera is looking at.
    /// </summary>
    Vector3 Target { get; set; }

    /// <summary>
    /// Gets the up direction vector (normalized).
    /// </summary>
    /// <remarks>
    /// This vector defines which direction is "up" for the camera.
    /// Usually (0, 1, 0) in world space.
    /// </remarks>
    Vector3 Up { get; }

    /// <summary>
    /// Gets the forward direction vector (normalized).
    /// </summary>
    /// <remarks>
    /// This vector points in the direction the camera is looking.
    /// Calculated from Position and Target, or from Rotation.
    /// </remarks>
    Vector3 Forward { get; }

    /// <summary>
    /// Gets the right direction vector (normalized).
    /// </summary>
    /// <remarks>
    /// This vector points to the right side of the camera.
    /// Calculated as: Right = Forward × Up (cross product).
    /// </remarks>
    Vector3 Right { get; }

    /// <summary>
    /// Gets or sets the field of view in radians.
    /// </summary>
    /// <remarks>
    /// Typical values:
    /// - 1.047 radians (60°) = normal human vision
    /// - 0.785 radians (45°) = narrow view (zoom in)
    /// - 1.571 radians (90°) = wide view (fish-eye effect)
    /// </remarks>
    float FieldOfView { get; set; }

    /// <summary>
    /// Gets or sets the aspect ratio (width/height).
    /// </summary>
    /// <remarks>
    /// Common values:
    /// - 16/9 = 1.777... (widescreen)
    /// - 4/3 = 1.333... (classic)
    /// - 21/9 = 2.333... (ultrawide)
    /// </remarks>
    float AspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the near clipping plane distance.
    /// </summary>
    /// <remarks>
    /// Objects closer than this distance are not rendered.
    /// Typical value: 0.1f
    /// Too small values can cause Z-fighting artifacts.
    /// </remarks>
    float NearPlane { get; set; }

    /// <summary>
    /// Gets or sets the far clipping plane distance.
    /// </summary>
    /// <remarks>
    /// Objects farther than this distance are not rendered.
    /// Typical value: 1000.0f
    /// Balance between render distance and depth buffer precision.
    /// </remarks>
    float FarPlane { get; set; }

    /// <summary>
    /// Gets the view matrix (transforms from world space to camera space).
    /// </summary>
    /// <remarks>
    /// This matrix moves and rotates the entire world so that the camera
    /// is at the origin looking down the Z axis.
    /// </remarks>
    Matrix4x4 View { get; }

    /// <summary>
    /// Gets the projection matrix (transforms from camera space to clip space).
    /// </summary>
    /// <remarks>
    /// This matrix applies perspective projection, making distant objects
    /// appear smaller. Creates the 3D depth effect.
    /// </remarks>
    Matrix4x4 Projection { get; }

    /// <summary>
    /// Gets the bounding frustum for frustum culling.
    /// </summary>
    /// <remarks>
    /// The frustum is the pyramid-shaped volume that the camera can see.
    /// Used to quickly reject objects outside the view for optimization.
    /// </remarks>
    BoundingFrustum Frustum { get; }

    /// <summary>
    /// Converts a screen point to a ray in world space for picking.
    /// </summary>
    /// <param name="screenPosition">Screen position in pixels.</param>
    /// <param name="viewport">The viewport dimensions.</param>
    /// <returns>Ray in world space originating from camera through the screen point.</returns>
    /// <remarks>
    /// Used for mouse picking: click on screen → get ray → raycast to find 3D objects.
    /// </remarks>
    Ray GetPickRay(Vector2 screenPosition, Viewport viewport);

    /// <summary>
    /// Makes the camera look at a specific target point.
    /// </summary>
    /// <param name="target">The target position in world space.</param>
    /// <param name="up">The up vector (usually (0, 1, 0)).</param>
    void LookAt(Vector3 target, Vector3 up);

    /// <summary>
    /// Moves the camera by a specific offset in world space.
    /// </summary>
    /// <param name="offset">Movement offset vector.</param>
    void Move(Vector3 offset);

    /// <summary>
    /// Moves the camera forward/backward along its forward axis.
    /// </summary>
    /// <param name="distance">Distance to move (positive = forward, negative = backward).</param>
    void MoveForward(float distance);

    /// <summary>
    /// Moves the camera left/right along its right axis.
    /// </summary>
    /// <param name="distance">Distance to move (positive = right, negative = left).</param>
    void MoveRight(float distance);

    /// <summary>
    /// Moves the camera up/down along the world up axis.
    /// </summary>
    /// <param name="distance">Distance to move (positive = up, negative = down).</param>
    void MoveUp(float distance);

    /// <summary>
    /// Rotates the camera by pitch, yaw, and roll angles.
    /// </summary>
    /// <param name="pitch">Rotation around X axis in radians (look up/down).</param>
    /// <param name="yaw">Rotation around Y axis in radians (look left/right).</param>
    /// <param name="roll">Rotation around Z axis in radians (tilt head left/right).</param>
    void Rotate(float pitch, float yaw, float roll);

    /// <summary>
    ///  Updates the camera state based on elapsed game time.
    /// </summary>
    /// <param name="gameTime"></param>
    void Update(GameTime gameTime);
}
