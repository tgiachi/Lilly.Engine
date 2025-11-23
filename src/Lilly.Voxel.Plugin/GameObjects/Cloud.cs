using Silk.NET.Maths;

namespace Lilly.Voxel.Plugin.GameObjects;

/// <summary>
/// Represents a single cloud instance with world position and size.
/// </summary>
public struct Cloud
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Cloud"/> struct.
    /// </summary>
    /// <param name="position">World position of the cloud center.</param>
    /// <param name="size">Scale applied to the unit cube mesh.</param>
    public Cloud(Vector3D<float> position, Vector3D<float> size)
    {
        Position = position;
        Size = size;
    }

    /// <summary>
    /// Gets or sets the world position of the cloud.
    /// </summary>
    public Vector3D<float> Position { get; set; }

    /// <summary>
    /// Gets or sets the size of the cloud.
    /// </summary>
    public Vector3D<float> Size { get; set; }
}
