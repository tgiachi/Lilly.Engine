using Silk.NET.Maths;

namespace Lilly.Rendering.Core.Primitives;

/// <summary>
/// Represents a viewing frustum for culling and visibility testing.
/// </summary>
public class BoundingFrustum
{
    private readonly Plane<float>[] _planes;

    /// <summary>
    /// Gets the near plane of the frustum.
    /// </summary>
    public Plane<float> Near => _planes[0];

    /// <summary>
    /// Gets the far plane of the frustum.
    /// </summary>
    public Plane<float> Far => _planes[1];

    /// <summary>
    /// Gets the left plane of the frustum.
    /// </summary>
    public Plane<float> Left => _planes[2];

    /// <summary>
    /// Gets the right plane of the frustum.
    /// </summary>
    public Plane<float> Right => _planes[3];

    /// <summary>
    /// Gets the top plane of the frustum.
    /// </summary>
    public Plane<float> Top => _planes[4];

    /// <summary>
    /// Gets the bottom plane of the frustum.
    /// </summary>
    public Plane<float> Bottom => _planes[5];

    /// <summary>
    /// Initializes a new instance of the BoundingFrustum class.
    /// </summary>
    /// <param name="viewProjectionMatrix">The combined view-projection matrix.</param>
    public BoundingFrustum(Matrix4X4<float> viewProjectionMatrix)
    {
        _planes = new Plane<float>[6];
        ExtractPlanes(viewProjectionMatrix);
    }

    /// <summary>
    /// Checks if a point is inside the frustum.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point is inside the frustum, false otherwise.</returns>
    public bool Contains(Vector3D<float> point)
    {
        foreach (var plane in _planes)
        {
            if (Plane.DotCoordinate(plane, point) < 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if a sphere intersects or is inside the frustum.
    /// </summary>
    /// <param name="center">The center of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <returns>True if the sphere intersects or is inside the frustum, false otherwise.</returns>
    public bool Intersects(Vector3D<float> center, float radius)
    {
        foreach (var plane in _planes)
        {
            var distance = Plane.DotCoordinate(plane, center);

            if (distance < -radius)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if an axis-aligned bounding box intersects or is inside the frustum.
    /// </summary>
    /// <param name="min">The minimum corner of the bounding box.</param>
    /// <param name="max">The maximum corner of the bounding box.</param>
    /// <returns>True if the box intersects or is inside the frustum, false otherwise.</returns>
    public bool Intersects(Vector3D<float> min, Vector3D<float> max)
    {
        foreach (var plane in _planes)
        {
            // Get the positive vertex (furthest point in the direction of the plane normal)
            var positiveVertex = new Vector3D<float>(
                plane.Normal.X >= 0 ? max.X : min.X,
                plane.Normal.Y >= 0 ? max.Y : min.Y,
                plane.Normal.Z >= 0 ? max.Z : min.Z
            );

            if (Plane.DotCoordinate(plane, positiveVertex) < 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a string representation of the frustum.
    /// </summary>
    /// <returns>A string describing the frustum.</returns>
    public override string ToString()
        => $"BoundingFrustum(Near: {Near}, Far: {Far}, Left: {Left}, Right: {Right}, Top: {Top}, Bottom: {Bottom})";

    /// <summary>
    /// Updates the frustum planes from a new view-projection matrix.
    /// </summary>
    /// <param name="viewProjectionMatrix">The combined view-projection matrix.</param>
    public void Update(Matrix4X4<float> viewProjectionMatrix)
    {
        ExtractPlanes(viewProjectionMatrix);
    }

    private void ExtractPlanes(Matrix4X4<float> matrix)
    {
        // Left plane
        _planes[2] = NormalizePlane(
            new(
                matrix.M14 + matrix.M11,
                matrix.M24 + matrix.M21,
                matrix.M34 + matrix.M31,
                matrix.M44 + matrix.M41
            )
        );

        // Right plane
        _planes[3] = NormalizePlane(
            new(
                matrix.M14 - matrix.M11,
                matrix.M24 - matrix.M21,
                matrix.M34 - matrix.M31,
                matrix.M44 - matrix.M41
            )
        );

        // Bottom plane
        _planes[5] = NormalizePlane(
            new(
                matrix.M14 + matrix.M12,
                matrix.M24 + matrix.M22,
                matrix.M34 + matrix.M32,
                matrix.M44 + matrix.M42
            )
        );

        // Top plane
        _planes[4] = NormalizePlane(
            new(
                matrix.M14 - matrix.M12,
                matrix.M24 - matrix.M22,
                matrix.M34 - matrix.M32,
                matrix.M44 - matrix.M42
            )
        );

        // Near plane
        _planes[0] = NormalizePlane(
            new(
                matrix.M14 + matrix.M13,
                matrix.M24 + matrix.M23,
                matrix.M34 + matrix.M33,
                matrix.M44 + matrix.M43
            )
        );

        // Far plane
        _planes[1] = NormalizePlane(
            new(
                matrix.M14 - matrix.M13,
                matrix.M24 - matrix.M23,
                matrix.M34 - matrix.M33,
                matrix.M44 - matrix.M43
            )
        );
    }

    private static Plane<float> NormalizePlane(Plane<float> plane)
    {
        var length = plane.Normal.Length;

        return new(
            plane.Normal / length,
            plane.Distance / length
        );
    }
}
