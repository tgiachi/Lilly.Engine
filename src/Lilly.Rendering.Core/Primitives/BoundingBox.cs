using System.Numerics;

namespace Lilly.Rendering.Core.Primitives;

/// <summary>
/// Represents an axis-aligned bounding box in 3D space defined by minimum and maximum points.
/// </summary>
public readonly struct BoundingBox
{
    /// <summary>
    /// Gets the minimum corner point of the bounding box.
    /// </summary>
    public Vector3 Min { get; }

    /// <summary>
    /// Gets the maximum corner point of the bounding box.
    /// </summary>
    public Vector3 Max { get; }

    /// <summary>
    /// Initializes a new bounding box with the specified minimum and maximum points.
    /// </summary>
    /// <param name="min">The minimum corner point.</param>
    /// <param name="max">The maximum corner point.</param>
    public BoundingBox(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Gets the center point of the bounding box.
    /// </summary>
    public Vector3 Center => (Min + Max) * 0.5f;

    /// <summary>
    /// Gets the size (extents) of the bounding box along each axis.
    /// </summary>
    public Vector3 Size => Max - Min;

    /// <summary>
    /// Checks if the bounding box contains the specified point.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point is inside or on the boundary of the box.</returns>
    public bool Contains(Vector3 point)
        => Min.X <= point.X &&
           point.X <= Max.X &&
           Min.Y <= point.Y &&
           point.Y <= Max.Y &&
           Min.Z <= point.Z &&
           point.Z <= Max.Z;

    /// <summary>
    /// Gets all eight corners of the bounding box.
    /// </summary>
    /// <param name="corners">Array to fill with corner points. Must have at least 8 elements.</param>
    /// <exception cref="ArgumentException">Thrown if the array has fewer than 8 elements.</exception>
    public void GetCorners(Vector3[] corners)
    {
        if (corners.Length < 8)
        {
            throw new ArgumentException("Corners array must have at least 8 elements.");
        }

        corners[0] = new(Min.X, Max.Y, Max.Z);
        corners[1] = new(Max.X, Max.Y, Max.Z);
        corners[2] = new(Max.X, Min.Y, Max.Z);
        corners[3] = new(Min.X, Min.Y, Max.Z);
        corners[4] = new(Min.X, Max.Y, Min.Z);
        corners[5] = new(Max.X, Max.Y, Min.Z);
        corners[6] = new(Max.X, Min.Y, Min.Z);
        corners[7] = new(Min.X, Min.Y, Min.Z);
    }

    /// <summary>
    /// Checks if this bounding box intersects with another bounding box.
    /// </summary>
    /// <param name="other">The other bounding box to test.</param>
    /// <returns>True if the boxes overlap, false otherwise.</returns>
    public bool Intersects(BoundingBox other)
        => Min.X <= other.Max.X &&
           Max.X >= other.Min.X &&
           Min.Y <= other.Max.Y &&
           Max.Y >= other.Min.Y &&
           Min.Z <= other.Max.Z &&
           Max.Z >= other.Min.Z;
}
