using System.Numerics;
using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Extensions;

public static class VectorExtensions
{

    public static Vector2 ToNumerics(this Vector2D<float> vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    public static Vector2D<float> ToSilk(this Vector2 vector)
    {
        return new Vector2D<float>(vector.X, vector.Y);
    }

    public static Matrix3x2 ToNumerics(this Matrix3X2<float> matrix)
    {
        return new Matrix3x2(
            matrix.M11, matrix.M12,
            matrix.M21, matrix.M22,
            matrix.M31, matrix.M32
        );
    }

    public static Matrix3X2<float> ToSilk(this Matrix3x2 matrix)
    {
        return new Matrix3X2<float>(
            matrix.M11, matrix.M12,
            matrix.M21, matrix.M22,
            matrix.M31, matrix.M32
        );
    }

}
