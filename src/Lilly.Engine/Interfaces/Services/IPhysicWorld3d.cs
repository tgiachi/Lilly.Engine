using System.Numerics;
using Lilly.Engine.Core.Interfaces.Services.Base;
using Lilly.Engine.Data.Physics;

namespace Lilly.Engine.Interfaces.Services;

/// <summary>
/// Abstraction over the 3D physics simulation. Implemented by the physics plugin (e.g., BEPU).
/// </summary>
public interface IPhysicWorld3d : ILillyService
{
    IPhysicsBodyHandle CreateDynamic(PhysicsBodyConfig config);
    IPhysicsBodyHandle CreateStatic(PhysicsShape shape, RigidPose pose);

    void Remove(IPhysicsBodyHandle handle);

    void SetPose(IPhysicsBodyHandle handle, RigidPose pose);
    RigidPose GetPose(IPhysicsBodyHandle handle);

    void SetVelocity(IPhysicsBodyHandle handle, Vector3 linear, Vector3 angular);
    void ApplyImpulse(IPhysicsBodyHandle handle, Vector3 impulse, Vector3 offset);

    /// <summary>
    /// Gets current gravity vector used by the physics world.
    /// </summary>
    Vector3 Gravity { get; }

    /// <summary>
    /// Updates gravity at runtime.
    /// </summary>
    void SetGravity(Vector3 gravity);

    /// <summary>
    /// Forces all dynamic bodies to wake up.
    /// </summary>
    void WakeAllBodies();

    PhysicsWorldStats GetStats();
}
