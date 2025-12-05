namespace Lilly.Engine.Data.Physics;

/// <summary>
/// Snapshot of physics world metrics.
/// </summary>
public readonly record struct PhysicsWorldStats(
    int DynamicBodies,
    int ActiveDynamicBodies,
    int StaticBodies,
    int ShapeCount,
    int ThreadCount,
    System.Numerics.Vector3 Gravity
);
