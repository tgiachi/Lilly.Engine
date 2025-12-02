using System.Numerics;

namespace Lilly.Rendering.Core.Primitives;

public struct BoundingBox
{
    public Vector3 Min;
    public Vector3 Max;

    public BoundingBox(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public Vector3 Center => (Min + Max) * 0.5f;
    public Vector3 Size => Max - Min;

    public bool Intersects(BoundingBox other)
    {
        return (Min.X <= other.Max.X && Max.X >= other.Min.X) &&
               (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y) &&
               (Min.Z <= other.Max.Z && Max.Z >= other.Min.Z);
    }

    public bool Contains(Vector3 point)
    {
        return (Min.X <= point.X && point.X <= Max.X) &&
               (Min.Y <= point.Y && point.Y <= Max.Y) &&
               (Min.Z <= point.Z && point.Z <= Max.Z);
    }
    
    public void GetCorners(Vector3[] corners)
    {
        if (corners.Length < 8)
            throw new ArgumentException("Corners array must have at least 8 elements.");

        corners[0] = new Vector3(Min.X, Max.Y, Max.Z);
        corners[1] = new Vector3(Max.X, Max.Y, Max.Z);
        corners[2] = new Vector3(Max.X, Min.Y, Max.Z);
        corners[3] = new Vector3(Min.X, Min.Y, Max.Z);
        corners[4] = new Vector3(Min.X, Max.Y, Min.Z);
        corners[5] = new Vector3(Max.X, Max.Y, Min.Z);
        corners[6] = new Vector3(Max.X, Min.Y, Min.Z);
        corners[7] = new Vector3(Min.X, Min.Y, Min.Z);
    }
}
