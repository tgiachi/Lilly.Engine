namespace Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

/// <summary>
/// Defines an interface for game objects that support object pooling and need to reset their state.
/// Implement this interface in game object classes to customize the reset behavior when objects are returned to the pool.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Resets the game object to its initial state when returned to the pool.
    /// This method is called by the pooling system to prepare the object for reuse.
    /// </summary>
    /// <remarks>
    /// Derived classes should override this method to reset any custom state or properties that might have been modified during
    /// gameplay.
    /// The base implementation handles clearing parent and children references.
    /// </remarks>
    void ResetForPooling();
}
