using Lilly.Engine.Rendering.Core.Primitives;

namespace Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

/// <summary>
/// Represents a 3D game object with spatial transformation and rendering properties.
/// </summary>
public interface IGameObject3D : IGameObject
{
    Transform3D Transform { get; }

    bool IsVisible { get; set; }

    int Layer { get; set; }
}
