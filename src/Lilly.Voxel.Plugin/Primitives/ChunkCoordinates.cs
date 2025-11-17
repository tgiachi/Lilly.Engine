namespace Lilly.Voxel.Plugin.Primitives;

/// <summary>
/// Represents the coordinates of a chunk in the voxel world grid.
/// </summary>
public struct ChunkCoordinates : IEquatable<ChunkCoordinates>
{
    /// <summary>
    /// Chunk X coordinate (horizontal).
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Chunk Y coordinate (vertical).
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Chunk Z coordinate (depth).
    /// </summary>
    public int Z { get; set; }

    /// <summary>
    /// Initializes a new chunk coordinate with X, Y, Z values.
    /// </summary>
    public ChunkCoordinates(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Gets the chunk coordinates to the right (X+1).
    /// </summary>
    public static ChunkCoordinates Right => new(1, 0, 0);

    /// <summary>
    /// Gets the chunk coordinates to the left (X-1).
    /// </summary>
    public static ChunkCoordinates Left => new(-1, 0, 0);

    /// <summary>
    /// Gets the chunk coordinates up (Y+1).
    /// </summary>
    public static ChunkCoordinates Up => new(0, 1, 0);

    /// <summary>
    /// Gets the chunk coordinates down (Y-1).
    /// </summary>
    public static ChunkCoordinates Down => new(0, -1, 0);

    /// <summary>
    /// Gets the chunk coordinates forward (Z+1).
    /// </summary>
    public static ChunkCoordinates Forward => new(0, 0, 1);

    /// <summary>
    /// Gets the chunk coordinates backward (Z-1).
    /// </summary>
    public static ChunkCoordinates Backward => new(0, 0, -1);

    /// <summary>
    /// Adds two chunk coordinates together.
    /// </summary>
    public static ChunkCoordinates operator +(ChunkCoordinates a, ChunkCoordinates b)
        => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>
    /// Subtracts one chunk coordinate from another.
    /// </summary>
    public static ChunkCoordinates operator -(ChunkCoordinates a, ChunkCoordinates b)
        => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>
    /// Multiplies a chunk coordinate by a scalar.
    /// </summary>
    public static ChunkCoordinates operator *(ChunkCoordinates a, int scale)
        => new(a.X * scale, a.Y * scale, a.Z * scale);

    /// <summary>
    /// Checks if two chunk coordinates are equal.
    /// </summary>
    public static bool operator ==(ChunkCoordinates a, ChunkCoordinates b)
        => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    /// <summary>
    /// Checks if two chunk coordinates are not equal.
    /// </summary>
    public static bool operator !=(ChunkCoordinates a, ChunkCoordinates b)
        => !(a == b);

    public bool Equals(ChunkCoordinates other)
        => X == other.X && Y == other.Y && Z == other.Z;

    public override bool Equals(object obj)
        => obj is ChunkCoordinates other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(X, Y, Z);

    public override string ToString()
        => $"({X}, {Y}, {Z})";
}
