using System.Numerics;
using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Extensions;

public static class VectorExtensions
{

    /// <summary>
    /// Converts a Silk.NET Vector2D to a System.Numerics Vector2.
    /// </summary>
    /// <param name="vector">The Silk.NET vector to convert.</param>
    /// <returns>The converted System.Numerics vector.</returns>
    public static Vector2 ToNumerics(this Vector2D<float> vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    /// <summary>
    /// Converts a System.Numerics Vector2 to a Silk.NET Vector2D.
    /// </summary>
    /// <param name="vector">The System.Numerics vector to convert.</param>
    /// <returns>The converted Silk.NET vector.</returns>
    public static Vector2D<float> ToSilk(this Vector2 vector)
    {
        return new Vector2D<float>(vector.X, vector.Y);
    }

    /// <summary>
    /// Converts a Silk.NET Matrix3X2 to a System.Numerics Matrix3x2.
    /// </summary>
    /// <param name="matrix">The Silk.NET matrix to convert.</param>
    /// <returns>The converted System.Numerics matrix.</returns>
    public static Matrix3x2 ToNumerics(this Matrix3X2<float> matrix)
    {
        return new Matrix3x2(
            matrix.M11, matrix.M12,
            matrix.M21, matrix.M22,
            matrix.M31, matrix.M32
        );
    }

    /// <summary>
    /// Converts a System.Numerics Matrix3x2 to a Silk.NET Matrix3X2.
    /// </summary>
    /// <param name="matrix">The System.Numerics matrix to convert.</param>
    /// <returns>The converted Silk.NET matrix.</returns>
    public static Matrix3X2<float> ToSilk(this Matrix3x2 matrix)
    {
        return new Matrix3X2<float>(
            matrix.M11, matrix.M12,
            matrix.M21, matrix.M22,
            matrix.M31, matrix.M32
        );
    }

}
