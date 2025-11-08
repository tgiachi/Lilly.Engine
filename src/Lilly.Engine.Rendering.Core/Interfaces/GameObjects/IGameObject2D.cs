using Lilly.Engine.Rendering.Core.Primitives;

namespace Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

public interface IGameObject2D : IGameObject
{
    Transform2D Transform { get; }

    bool IsVisible { get; set; }

    int Layer { get; set; }
}
