using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Primitives;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Interfaces.Camera;

public interface ICamera3D : IUpdatable
{
    /// <summary>
    /// Gets or sets whether the camera is enabled
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the camera name
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the camera position in world space
    /// </summary>
    Vector3D<float> Position { get; set; }

    /// <summary>
    /// Gets or sets the camera rotation
    /// </summary>
    Quaternion<float> Rotation { get; set; }

    /// <summary>
    /// Gets or sets the target point the camera is looking at
    /// </summary>
    Vector3D<float> Target { get; set; }

    /// <summary>
    /// Gets the up direction vector
    /// </summary>
    Vector3D<float> Up { get; }

    /// <summary>
    /// Gets the forward direction vector
    /// </summary>
    Vector3D<float> Forward { get; }

    /// <summary>
    /// Gets the right direction vector
    /// </summary>
    Vector3D<float> Right { get; }

    /// <summary>
    /// Gets or sets the field of view in radians
    /// </summary>
    float FieldOfView { get; set; }

    /// <summary>
    /// Gets or sets the aspect ratio (width/height)
    /// </summary>
    float AspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the near clipping plane distance
    /// </summary>
    float NearPlane { get; set; }

    /// <summary>
    /// Gets or sets the far clipping plane distance
    /// </summary>
    float FarPlane { get; set; }

    /// <summary>
    /// Gets the view matrix
    /// </summary>
    Matrix4X4<float> View { get; }

    /// <summary>
    /// Gets the projection matrix
    /// </summary>
    Matrix4X4<float> Projection { get; }

    /// <summary>
    /// Gets the bounding frustum for frustum culling
    /// </summary>
    BoundingFrustum Frustum { get; }

    /// <summary>
    /// Converts a screen point to a ray in world space for picking
    /// </summary>
    /// <param name="screenPosition">Screen position in pixels</param>
    /// <param name="viewport">The viewport</param>
    /// <returns>Ray in world space</returns>
    Ray GetPickRay(Vector2D<int> screenPosition, Viewport viewport);

    /// <summary>
    /// Makes the camera look at a specific target point
    /// </summary>
    /// <param name="target">The target position</param>
    /// <param name="up">The up vector</param>
    void LookAt(Vector3D<float> target, Vector3D<float> up);

    void LookAt(IGameObject3D targetObject);

    /// <summary>
    /// Moves the camera by a specific offset
    /// </summary>
    /// <param name="offset">Movement offset in world space</param>
    void Move(Vector3D<float> offset);

    /// <summary>
    /// Moves the camera forward/backward along the forward axis
    /// </summary>
    /// <param name="distance">Distance to move (negative = backward)</param>
    void MoveForward(float distance);

    /// <summary>
    /// Moves the camera left/right along the right axis
    /// </summary>
    /// <param name="distance">Distance to move (negative = left)</param>
    void MoveRight(float distance);

    /// <summary>
    /// Moves the camera up/down along the world up axis
    /// </summary>
    /// <param name="distance">Distance to move (negative = down)</param>
    void MoveUp(float distance);

    /// <summary>
    /// Rotates the camera by pitch, yaw, and roll angles
    /// </summary>
    /// <param name="pitch">Rotation around X axis in radians</param>
    /// <param name="yaw">Rotation around Y axis in radians</param>
    /// <param name="roll">Rotation around Z axis in radians</param>
    void Rotate(float pitch, float yaw, float roll);


    /// <summary>
    ///  Determines if a 3D game object is within the camera's frustum
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    bool IsInFrustum(IGameObject3D gameObject);
}
