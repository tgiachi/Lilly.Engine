using Lilly.Engine.Data.Physics;
using Lilly.Rendering.Core.Primitives;

namespace Lilly.Engine.Interfaces.Physics;

public interface IPhysicsGameObject3d
{
    bool IsStatic { get; }

    /// <summary>
    /// Raised when the collision shape needs to be rebuilt (e.g., mesh or size changed).
    /// </summary>
    event Action? PhysicsShapeDirty;

    /// <summary>
    /// Transform that should be kept in sync with the physics body.
    /// </summary>
    Transform3D PhysicsTransform { get; }

    /// <summary>
    /// Controls how the transform is synchronized from the physics simulation.
    /// </summary>
    PhysicsSyncMode SyncMode => PhysicsSyncMode.FullPose;

    PhysicsBodyConfig BuildBodyConfig();

    void OnPhysicsAttached(IPhysicsBodyHandle h);

    /// <summary>
    /// Optional callback invoked when the physics body is detached.
    /// </summary>
    void OnPhysicsDetached() { }
}
