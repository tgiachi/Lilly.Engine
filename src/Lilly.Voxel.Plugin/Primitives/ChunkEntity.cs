using System.Numerics;
using Lilly.Voxel.Plugin.Types;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Primitives;

/// <summary>
/// Stores the runtime state for a cubic chunk, including its position and contained blocks.
/// </summary>
public class ChunkEntity
{
    /// <summary>
    /// Number of blocks along the X and Z axes.
    /// </summary>
    public const int Size = 32;

    /// <summary>
    /// Number of blocks along the Y axis.
    /// </summary>
    public const int Height = 32;


    /// <summary>
    ///  Gets or sets a value indicating whether the chunk has been modified since last save.
    /// </summary>
    public bool IsModified { get; set; }

    /// <summary>
    /// Gets the world position at which the chunk is anchored.
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// Gets the chunk coordinates in the voxel world grid.
    /// </summary>
    public Vector3 ChunkCoordinates { get; }

    /// <summary>
    /// Gets or sets the current state of this chunk.
    /// </summary>
    public ChunkState State { get; set; } = ChunkState.Unloaded;

    /// <summary>
    /// Gets or sets a value indicating whether the mesh needs to be regenerated.
    /// </summary>
    public bool IsMeshDirty { get; set; } = true;


    /// <summary>
    /// Initializes a new <see cref="ChunkEntity"/> at the provided chunk coordinates.
    /// </summary>
    /// <param name="coordinates">Chunk coordinates in the world grid.</param>
    public ChunkEntity(Vector3 coordinates)
    {
        Blocks = new ushort[Size * Size * Height];
        LightLevels = new byte[Size * Size * Height];
        // LightColors is lazy loaded
        // coordinates passed in are world-space chunk origin (already multiplied by Size/Height)
        Position = coordinates;
        ChunkCoordinates = new Vector3(
            coordinates.X / Size,
            coordinates.Y / Height,
            coordinates.Z / Size
        );

        Array.Fill(LightLevels, (byte)15);
    }

    /// <summary>
    /// Gets the raw backing array that stores blocks for the chunk.
    /// </summary>
    public ushort[] Blocks { get; }

    /// <summary>
    /// Gets the raw backing array that stores light levels for the chunk.
    /// </summary>
    public byte[] LightLevels { get; }

    /// <summary>
    /// Gets the raw backing array that stores light colors for the chunk (for colored light propagation).
    /// Null if no colored lights are present (defaults to White).
    /// </summary>
    public Color4b[]? LightColors { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the lighting needs to be recalculated.
    /// </summary>
    public bool IsLightingDirty { get; set; } = true;

    /// <summary>
    /// Retrieves the block stored at the specified coordinates without bounds checking.
    /// USE ONLY when you are certain x, y, z are valid (0 to Size/Height-1).
    /// </summary>
    public ushort GetBlockFast(int x, int y, int z)
    {
        return Blocks[x + y * Size + z * Size * Height];
    }

    /// <summary>
    /// Retrieves the block stored at the specified coordinates.
    /// </summary>
    /// <param name="x">Block coordinate along X.</param>
    /// <param name="y">Block coordinate along Y.</param>
    /// <param name="z">Block coordinate along Z.</param>
    /// <returns>The block entity at the given coordinates.</returns>
    public ushort GetBlock(int x, int y, int z)
    {
        return Blocks[GetIndex(x, y, z)];
    }

    /// <summary>
    /// Sets the block at the specified coordinates without bounds checking.
    /// USE ONLY when you are certain x, y, z are valid.
    /// </summary>
    public void SetBlockFast(int x, int y, int z, ushort blockId)
    {
        Blocks[x + y * Size + z * Size * Height] = blockId;
    }

    /// <summary>
    /// Stores a block at the specified coordinates, replacing any previous value.
    /// </summary>
    /// <param name="x">Block coordinate along X.</param>
    /// <param name="y">Block coordinate along Y.</param>
    /// <param name="z">Block coordinate along Z.</param>
    /// <param name="block">Block entity to store.</param>
    public void SetBlock(int x, int y, int z, ushort block)
    {
        Blocks[GetIndex(x, y, z)] = block;
    }

    /// <summary>
    /// Retrieves the block at the specified vector position.
    /// </summary>
    /// <param name="position">Vector position of the block.</param>
    /// <returns>The block entity at the position.</returns>
    public ushort GetBlock(Vector3 position)
    {
        return GetBlock((int)position.X, (int)position.Y, (int)position.Z);
    }

    /// <summary>
    /// Stores a block at the position represented by the vector.
    /// </summary>
    /// <param name="position">Vector position of the block.</param>
    /// <param name="block">Block entity to store.</param>
    public void SetBlock(Vector3 position, ushort block)
    {
        SetBlock((int)position.X, (int)position.Y, (int)position.Z, block);
    }

    /// <summary>
    /// Retrieves the block stored at the specified linear index.
    /// </summary>
    /// <param name="index">Zero-based linear index into the chunk.</param>
    /// <returns>The block entity at the given index.</returns>
    public ushort GetBlock(int index)
    {
        ValidateIndex(index);
        return Blocks[index];
    }

    /// <summary>
    /// Stores a block at the specified linear index.
    /// </summary>
    /// <param name="index">Zero-based linear index.</param>
    /// <param name="block">Block entity to store.</param>
    public void SetBlock(int index, ushort block)
    {
        ValidateIndex(index);
        Blocks[index] = block;
    }

    /// <summary>
    /// Calculates the linear index for the provided block coordinates.
    /// </summary>
    /// <param name="x">Block coordinate along X.</param>
    /// <param name="y">Block coordinate along Y.</param>
    /// <param name="z">Block coordinate along Z.</param>
    /// <returns>The corresponding linear index.</returns>
    public static int GetIndex(int x, int y, int z)
    {
        ValidateCoordinates(x, y, z);
        return x + y * Size + z * Size * Height;
    }

    /// <summary>
    /// Calculates the linear index for the provided vector position.
    /// </summary>
    /// <param name="position">Vector position of the block.</param>
    /// <returns>The corresponding linear index.</returns>
    public int GetIndex(Vector3 position)
    {
        return GetIndex((int)position.X, (int)position.Y, (int)position.Z);
    }


    /// <summary>
    /// Provides array-style access to blocks using explicit coordinates.
    /// </summary>
    public ushort this[int x, int y, int z]
    {
        get => GetBlock(x, y, z);
        set => SetBlock(x, y, z, value);
    }

    /// <summary>
    /// Provides array-style access to blocks using a vector position.
    /// </summary>
    public ushort this[Vector3 position]
    {
        get => GetBlock(position);
        set => SetBlock(position, value);
    }

    public byte GetLightLevel(int x, int y, int z)
    {
        return LightLevels[GetIndex(x, y, z)];
    }

    public void SetLightLevel(int x, int y, int z, byte level)
    {
        LightLevels[GetIndex(x, y, z)] = level;
    }

    public void SetLightLevels(byte[] levels)
    {
        if (levels.Length != LightLevels.Length)
        {
            throw new ArgumentException($"Light levels array must have length {LightLevels.Length}", nameof(levels));
        }

        Array.Copy(levels, LightLevels, levels.Length);
    }

    /// <summary>
    /// Retrieves the light color at the specified coordinates.
    /// </summary>
    public Color4b GetLightColor(int x, int y, int z)
    {
        if (LightColors == null)
        {
            return Color4b.White;
        }
        return LightColors[GetIndex(x, y, z)];
    }

    /// <summary>
    /// Sets the light color at the specified coordinates.
    /// </summary>
    public void SetLightColor(int x, int y, int z, Color4b color)
    {
        if (LightColors == null)
        {
            // Optimization: If setting to white and array is null, do nothing
            if (color == Color4b.White)
            {
                return;
            }
            InitializeLightColors();
        }
        LightColors![GetIndex(x, y, z)] = color;
    }

    /// <summary>
    /// Sets all light colors from the provided array.
    /// </summary>
    public void SetLightColors(Color4b[] colors)
    {
        int length = Size * Size * Height;
        if (colors.Length != length)
        {
            throw new ArgumentException($"Light colors array must have length {length}", nameof(colors));
        }

        if (LightColors == null)
        {
            LightColors = new Color4b[length];
        }

        Array.Copy(colors, LightColors, colors.Length);
    }

    private void InitializeLightColors()
    {
        int length = Size * Size * Height;
        LightColors = new Color4b[length];
        Array.Fill(LightColors, Color4b.White);
    }

    public bool IsInBounds(int x, int y, int z)
    {
        return x >= 0 && x < Size &&
               y >= 0 && y < Height &&
               z >= 0 && z < Size;
    }

    public bool IsInBounds(Vector3 position)
    {
        return IsInBounds((int)position.X, (int)position.Y, (int)position.Z);
    }

    /// <summary>
    /// Validates that the provided coordinates fall within chunk bounds.
    /// </summary>
    /// <param name="x">Block coordinate along X.</param>
    /// <param name="y">Block coordinate along Y.</param>
    /// <param name="z">Block coordinate along Z.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any coordinate is outside the chunk dimensions.</exception>
    private static void ValidateCoordinates(int x, int y, int z)
    {
        if ((uint)x >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, $"Expected 0 <= x < {Size}.");
        }

        if ((uint)y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, $"Expected 0 <= y < {Height}.");
        }

        if ((uint)z >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(z), z, $"Expected 0 <= z < {Size}.");
        }
    }

    public override string ToString()
    {
        return $"{Position} ({Size})";
    }

    /// <summary>
    /// Validates that the provided index falls within the bounds of the chunk.
    /// </summary>
    /// <param name="index">Linear index into the block array.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is outside the chunk range.</exception>
    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)Blocks.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Expected 0 <= index < {Blocks.Length}.");
        }
    }

    /// <summary>
    /// Clears all blocks in the chunk, setting them to air (block ID 0).
    /// Marks the chunk mesh as dirty for regeneration.
    /// </summary>
    public void Clear()
    {
        Array.Fill(Blocks, (ushort)0);  // 0 = Air block ID
        IsMeshDirty = true;
    }

    /// <summary>
    /// Attempts to get the block adjacent to the specified coordinates in the given direction.
    /// </summary>
    /// <param name="x">X coordinate of the source block.</param>
    /// <param name="y">Y coordinate of the source block.</param>
    /// <param name="z">Z coordinate of the source block.</param>
    /// <param name="face">The direction to check (which adjacent block).</param>
    /// <param name="block">The adjacent block ID if found within this chunk, otherwise 0 (air).</param>
    /// <returns>
    /// True if the adjacent block is within this chunk's bounds;
    /// False if the adjacent block would be in a neighboring chunk.
    /// </returns>
    public bool TryGetAdjacentBlock(int x, int y, int z, BlockFace face, out ushort block)
    {
        block = 0;  // Default to air

        // Calculate adjacent coordinates based on face direction
        var (adjX, adjY, adjZ) = face switch
        {
            BlockFace.Top => (x, y + 1, z),           // Up: +Y
            BlockFace.Bottom => (x, y - 1, z),        // Down: -Y
            BlockFace.Right => (x + 1, y, z),         // Right: +X
            BlockFace.Left => (x - 1, y, z),          // Left: -X
            BlockFace.Front => (x, y, z + 1),         // Forward: +Z
            BlockFace.Back => (x, y, z - 1),          // Backward: -Z
            _ => (x, y, z)  // Invalid face
        };

        // Check if adjacent coordinates are within this chunk's bounds
        if (IsInBounds(adjX, adjY, adjZ))
        {
            block = GetBlock(adjX, adjY, adjZ);
            return true;  // Block is within this chunk
        }

        return false;  // Block is outside this chunk (in a neighbor)
    }
}
