using Lilly.Engine.Rendering.Core.Primitives;

namespace Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

/// <summary>
/// Represents a 2D game object with spatial transformation and rendering properties.
/// </summary>
public interface IGameObject2D : IGameObject
{
    Transform2D Transform { get; }

    bool IsVisible { get; set; }

    int Layer { get; set; }
}
