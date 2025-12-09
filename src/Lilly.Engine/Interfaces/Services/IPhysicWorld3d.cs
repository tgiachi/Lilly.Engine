using System.Numerics;
using Lilly.Engine.Core.Interfaces.Services.Base;
using Lilly.Engine.Data.Physics;

namespace Lilly.Engine.Interfaces.Services;

/// <summary>
/// Abstraction over the 3D physics simulation. Implemented by the physics plugin (e.g., BEPU).
/// </summary>
public interface IPhysicWorld3d : ILillyService
{
    /// <summary>
    /// Gets current gravity vector used by the physics world.
    /// </summary>
    Vector3 Gravity { get; }

    void ApplyImpulse(IPhysicsBodyHandle handle, Vector3 impulse, Vector3 offset);
    IPhysicsBodyHandle CreateDynamic(PhysicsBodyConfig config);
    IPhysicsBodyHandle CreateStatic(PhysicsShape shape, RigidPose pose);
    RigidPose GetPose(IPhysicsBodyHandle handle);

    PhysicsWorldStats GetStats();

    void Remove(IPhysicsBodyHandle handle);

    /// <summary>
    /// Updates gravity at runtime.
    /// </summary>
    void SetGravity(Vector3 gravity);

    void SetPose(IPhysicsBodyHandle handle, RigidPose pose);

    void SetVelocity(IPhysicsBodyHandle handle, Vector3 linear, Vector3 angular);

    /// <summary>
    /// Forces all dynamic bodies to wake up.
    /// </summary>
    void WakeAllBodies();
}
