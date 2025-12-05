namespace Lilly.Engine.Data.Physics;

/// <summary>
/// Defines how a physics body should synchronize its transform back to the game object.
/// </summary>
public enum PhysicsSyncMode
{
    /// <summary>
    /// Updates both position and rotation from the physics pose.
    /// </summary>
    FullPose,

    /// <summary>
    /// Updates only the position, leaving rotation untouched.
    /// </summary>
    PositionOnly,

    /// <summary>
    /// Disables automatic synchronization.
    /// </summary>
    None
}
