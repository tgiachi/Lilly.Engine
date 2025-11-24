using Silk.NET.Maths;

namespace Lilly.Rendering.Core.Primitives;

/// <summary>
/// Represents a ray in 3D space with an origin point and a direction.
/// </summary>
public struct Ray
{
    /// <summary>
    /// Gets or sets the origin point of the ray.
    /// </summary>
    public Vector3D<float> Origin { get; set; }

    /// <summary>
    /// Gets or sets the direction vector of the ray (should be normalized).
    /// </summary>
    public Vector3D<float> Direction { get; set; }

    /// <summary>
    /// Initializes a new instance of the Ray struct.
    /// </summary>
    /// <param name="origin">The origin point of the ray.</param>
    /// <param name="direction">The direction vector of the ray.</param>
    public Ray(Vector3D<float> origin, Vector3D<float> direction)
    {
        Origin = origin;
        Direction = direction;
    }

    /// <summary>
    /// Gets a point along the ray at the specified distance from the origin.
    /// </summary>
    /// <param name="distance">Distance from the origin.</param>
    /// <returns>A point along the ray.</returns>
    public readonly Vector3D<float> GetPoint(float distance)
        => Origin + Direction * distance;

    /// <summary>
    /// Checks if this ray intersects with a plane.
    /// </summary>
    /// <param name="planeNormal">The normal vector of the plane.</param>
    /// <param name="planePoint">A point on the plane.</param>
    /// <param name="distance">The distance from the ray origin to the intersection point, if any.</param>
    /// <returns>True if the ray intersects the plane, false otherwise.</returns>
    public readonly bool IntersectsPlane(Vector3D<float> planeNormal, Vector3D<float> planePoint, out float distance)
    {
        var denominator = Vector3D.Dot(Direction, planeNormal);

        // Ray is parallel to the plane
        if (Math.Abs(denominator) < float.Epsilon)
        {
            distance = 0;

            return false;
        }

        var numerator = Vector3D.Dot(planePoint - Origin, planeNormal);
        distance = numerator / denominator;

        return distance >= 0;
    }

    /// <summary>
    /// Returns a string representation of the ray.
    /// </summary>
    /// <returns>A string describing the ray.</returns>
    public override readonly string ToString()
        => $"Ray(Origin: {Origin}, Direction: {Direction})";
}
