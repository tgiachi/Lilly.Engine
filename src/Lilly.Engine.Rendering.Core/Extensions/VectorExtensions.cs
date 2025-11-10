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



}
