using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Primitives;

public class Transform2D
{
    public Vector2D<float> Position { get; set; } = Vector2D<float>.Zero;
    public float Rotation { get; set; } = 0f;
    public Vector2D<float> Scale { get; set; } = Vector2D<float>.One;
}
