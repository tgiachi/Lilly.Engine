using System.Numerics;
using Silk.NET.Maths;

namespace Lilly.Rendering.Core.Extensions;

public static class VectorExtensions
{
    public static Vector2D<int> ToSilk(this Vector2 vector)
        => new((int)vector.X, (int)vector.Y);
}
