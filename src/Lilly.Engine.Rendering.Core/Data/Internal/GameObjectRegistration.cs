namespace Lilly.Engine.Rendering.Core.Data.Internal;

public record GameObjectRegistration (Type Type, bool UseObjectPooling = false);
