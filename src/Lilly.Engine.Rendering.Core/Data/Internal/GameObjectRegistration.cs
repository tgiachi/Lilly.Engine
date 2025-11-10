namespace Lilly.Engine.Rendering.Core.Data.Internal;

/// <summary>
/// Record containing information about a registered game object type.
/// </summary>
public record GameObjectRegistration(Type Type, bool UseObjectPooling = false);
