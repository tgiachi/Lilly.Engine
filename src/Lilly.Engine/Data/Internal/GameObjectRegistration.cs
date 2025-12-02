namespace Lilly.Engine.Data.Internal;

/// <summary>
/// Record containing information about a registered game object type.
/// </summary>
/// <param name="Type">The type of the game object to register.</param>
public record GameObjectRegistration(Type Type);
