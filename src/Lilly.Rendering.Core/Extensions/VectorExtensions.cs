using System.Numerics;
using Silk.NET.Maths;

namespace Lilly.Rendering.Core.Extensions;

/// <summary>
/// Provides extension methods for vector types.
/// </summary>
public static class VectorExtensions
{
    /// <summary>
    /// Converts a System.Numerics.Vector2 to Silk.NET.Maths.Vector2D&lt;int&gt;.
    /// </summary>
    /// <param name="vector">The vector to convert.</param>
    /// <returns>The converted vector.</returns>
    public static Vector2D<int> ToSilk(this Vector2 vector)
        => new((int)vector.X, (int)vector.Y);
}
