using Lilly.Engine.Rendering.Core.Primitives;

namespace Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

public interface IGameObject3D : IGameObject
{
    Transform3D Transform { get; }

    bool IsVisible { get; set; }

    int Layer { get; set; }
}
