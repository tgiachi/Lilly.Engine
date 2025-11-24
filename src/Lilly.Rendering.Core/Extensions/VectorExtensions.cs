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

    /// <summary>
    /// Converts a System.Numerics.Vector3 to Silk.NET.Maths.Vector3D&lt;float&gt;.
    /// </summary>
    /// <param name="vector">The vector to convert.</param>
    /// <returns>The converted vector.</returns>
    public static Vector3D<float> ToSilk(this Vector3 vector)
        => new(vector.X, vector.Y, vector.Z);

    /// <summary>
    /// Converts a System.Numerics.Matrix4x4 to Silk.NET.Maths.Matrix4X4&lt;float&gt;.
    /// </summary>
    /// <param name="matrix">The matrix to convert.</param>
    /// <returns>The converted matrix.</returns>
    public static Matrix4X4<float> ToSilk(this Matrix4x4 matrix)
        => new(
            new Vector4D<float>(matrix.M11, matrix.M12, matrix.M13, matrix.M14),
            new Vector4D<float>(matrix.M21, matrix.M22, matrix.M23, matrix.M24),
            new Vector4D<float>(matrix.M31, matrix.M32, matrix.M33, matrix.M34),
            new Vector4D<float>(matrix.M41, matrix.M42, matrix.M43, matrix.M44)
        );
}
