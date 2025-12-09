using System.Numerics;

namespace Lilly.Engine.Data.Physics;

/// <summary>
/// Position and orientation for a rigid body.
/// </summary>
/// <param name="Position">World position.</param>
/// <param name="Rotation">World rotation.</param>
public record RigidPose(Vector3 Position, Quaternion Rotation);

/// <summary>
/// Represents a handle to a body managed by the 3D physics world.
/// </summary>
public sealed record PhysicsBodyHandle(int Id) : IPhysicsBodyHandle;

/// <summary>
/// Represents a handle to a body managed by the 3D physics world.
/// </summary>
public interface IPhysicsBodyHandle
{
    int Id { get; }
}

/// <summary>
/// Base shape for a rigid body. Use pattern matching on the derived records.
/// </summary>
public abstract record PhysicsShape;

/// <summary>
/// Axis-aligned box shape.
/// </summary>
/// <param name="Width">X size.</param>
/// <param name="Height">Y size.</param>
/// <param name="Depth">Z size.</param>
public record BoxShape(float Width, float Height, float Depth) : PhysicsShape;

/// <summary>
/// Sphere shape.
/// </summary>
/// <param name="Radius">Sphere radius.</param>
public record SphereShape(float Radius) : PhysicsShape;

/// <summary>
/// Capsule shape aligned to the Y axis.
/// </summary>
/// <param name="Radius">Capsule radius.</param>
/// <param name="Length">Length of the cylindrical part (excluding hemispheres).</param>
public record CapsuleShape(float Radius, float Length) : PhysicsShape;

/// <summary>
/// Triangle mesh shape; best used for static geometry.
/// </summary>
/// <param name="Vertices">Mesh vertices.</param>
/// <param name="Indices">Triangle indices (wound counter-clockwise).</param>
public record MeshShape(IReadOnlyList<Vector3> Vertices, IReadOnlyList<int> Indices) : PhysicsShape;

/// <summary>
/// Convex hull shape built from a set of points.
/// </summary>
/// <param name="Vertices">Hull vertices.</param>
public record ConvexHullShape(IReadOnlyList<Vector3> Vertices) : PhysicsShape;

/// <summary>
/// Configuration used to create a dynamic body.
/// </summary>
/// <param name="Shape">Collision shape.</param>
/// <param name="Mass">Body mass.</param>
/// <param name="Pose">Initial pose.</param>
/// <param name="SpeculativeMargin">Collision speculative margin.</param>
public record PhysicsBodyConfig(PhysicsShape Shape, float Mass, RigidPose Pose, float SpeculativeMargin = 0.1f);
