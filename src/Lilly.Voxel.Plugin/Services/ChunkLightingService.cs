using Silk.NET.Maths;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Services;

/// <summary>
/// Provides lighting calculations for chunk mesh generation using ambient occlusion.
/// </summary>
public sealed class ChunkLightingService
{
    private readonly IBlockRegistry _blockRegistry;

    public ChunkLightingService(IBlockRegistry blockRegistry)
    {
        _blockRegistry = blockRegistry;
    }

    /// <summary>
    /// Computes the face lighting color using ambient occlusion heuristics.
    /// AO is calculated by checking the 3 adjacent blocks at each corner of the face.
    /// </summary>
    /// <param name="chunk">Chunk containing the block.</param>
    /// <param name="x">Block X coordinate.</param>
    /// <param name="y">Block Y coordinate.</param>
    /// <param name="z">Block Z coordinate.</param>
    /// <param name="face">Face being rendered.</param>
    /// <returns>Lighting color as Vector4D(R, G, B, FaceDirection) where each component is 0-255.</returns>
    public Vector4D<byte> CalculateFaceColor(ChunkEntity? chunk, int x, int y, int z, BlockFace face)
    {
        if (chunk == null)
        {
            // Default white with face direction
            return new Vector4D<byte>(255, 255, 255, GetDirectionIndex(face));
        }

        // Calculate AO at each corner of the face
        float ao0 = CalculateCornerAO(chunk, x, y, z, face, 0);
        float ao1 = CalculateCornerAO(chunk, x, y, z, face, 1);
        float ao2 = CalculateCornerAO(chunk, x, y, z, face, 2);
        float ao3 = CalculateCornerAO(chunk, x, y, z, face, 3);

        // Average the AO values
        float averageAO = (ao0 + ao1 + ao2 + ao3) / 4f;
        averageAO = Math.Clamp(averageAO, 0f, 1f);

        bool openSky = HasOpenSky(chunk, x, y, z);

        // Sky light (AO modulated, damped if underground)
        float skyLight = openSky ? averageAO : averageAO * 0.08f;

        // If the column above is blocked, allow some skylight when neighboring columns are exposed.
        if (!openSky && IsSideFace(face) && HasAdjacentSky(chunk, x, y, z, face))
        {
            skyLight = Math.Max(skyLight, averageAO * 0.45f);
        }

        // Baked/propagated light level (0-15). Treat the default "unlit" state (15 with dirty lighting) as 0
        // so we don't wash out caves before lighting is computed.
        var lightLevel = chunk.GetLightLevel(x, y, z);
        float levelFactor = Math.Clamp(lightLevel / 15f, 0f, 1f);

        if (chunk.IsLightingDirty && lightLevel >= 15)
        {
            levelFactor = 0f;
        }

        // Use the stronger of propagated light and skylight so torches still light caves.
        float light = Math.Max(levelFactor, skyLight);

        // Apply per-channel light color (defaults to white)
        var color = chunk.GetLightColor(x, y, z);
        float r = (color.R / 255f) * light;
        float g = (color.G / 255f) * light;
        float b = (color.B / 255f) * light;

        byte rByte = (byte)(Math.Clamp(r, 0f, 1f) * 255f);
        byte gByte = (byte)(Math.Clamp(g, 0f, 1f) * 255f);
        byte bByte = (byte)(Math.Clamp(b, 0f, 1f) * 255f);

        return new Vector4D<byte>(rByte, gByte, bByte, GetDirectionIndex(face));
    }

    /// <summary>
    /// Calculates ambient occlusion at a specific corner of a face.
    /// Each corner checks 3 adjacent blocks for occlusion.
    /// </summary>
    private static float CalculateCornerAO(ChunkEntity chunk, int x, int y, int z, BlockFace face, int corner)
    {
        // Each corner checks 3 adjacent blocks for occlusion
        int solidCount = 0;

        // Offsets for the 3 blocks that contribute to each corner
        var offsets = GetCornerOffsets(face, corner);

        foreach (var (dx, dy, dz) in offsets)
        {
            int checkX = x + dx;
            int checkY = y + dy;
            int checkZ = z + dz;

            // Check bounds
            if (checkX < 0 || checkX >= ChunkEntity.Size ||
                checkY < 0 || checkY >= ChunkEntity.Height ||
                checkZ < 0 || checkZ >= ChunkEntity.Size)
            {
                // Out of bounds = assume solid (conservative approach)
                solidCount++;
                continue;
            }

            ushort neighborBlockId = chunk.GetBlock(checkX, checkY, checkZ);
            if (neighborBlockId > 0) // Any non-air block
            {
                solidCount++;
            }
        }

        // AO brightness: 3 solids = 0.3 (70% dark), 0 solids = 1.0 (fully bright)
        return 1.0f - (solidCount / 3.0f * 0.7f);
    }

    /// <summary>
    /// Gets the three block offsets for a specific corner of a specific face.
    /// Each corner of a face is influenced by 3 adjacent blocks.
    /// </summary>
    private static (int dx, int dy, int dz)[] GetCornerOffsets(BlockFace face, int corner)
    {
        return face switch
        {
            BlockFace.Top => corner switch
            {
                0 => new[] { (-1, 0, -1), (-1, 0, 0), (0, 0, -1) }, // Corner at (x, y+1, z)
                1 => new[] { (-1, 0, 0), (-1, 0, 1), (0, 0, 1) },   // Corner at (x, y+1, z+1)
                2 => new[] { (0, 0, 1), (1, 0, 1), (1, 0, 0) },     // Corner at (x+1, y+1, z+1)
                3 => new[] { (1, 0, 0), (1, 0, -1), (0, 0, -1) },   // Corner at (x+1, y+1, z)
                _ => new[] { (0, 0, 0), (0, 0, 0), (0, 0, 0) }
            },
            BlockFace.Bottom => corner switch
            {
                0 => new[] { (-1, 0, -1), (-1, 0, 0), (0, 0, -1) },
                1 => new[] { (-1, 0, 0), (-1, 0, 1), (0, 0, 1) },
                2 => new[] { (0, 0, 1), (1, 0, 1), (1, 0, 0) },
                3 => new[] { (1, 0, 0), (1, 0, -1), (0, 0, -1) },
                _ => new[] { (0, 0, 0), (0, 0, 0), (0, 0, 0) }
            },
            BlockFace.Front => corner switch
            {
                0 => new[] { (-1, -1, 0), (-1, 0, 0), (0, -1, 0) },  // Corner at (x, y, z+1)
                1 => new[] { (-1, 0, 0), (-1, 1, 0), (0, 1, 0) },    // Corner at (x, y+1, z+1)
                2 => new[] { (0, 1, 0), (1, 1, 0), (1, 0, 0) },      // Corner at (x+1, y+1, z+1)
                3 => new[] { (1, 0, 0), (1, -1, 0), (0, -1, 0) },    // Corner at (x+1, y, z+1)
                _ => new[] { (0, 0, 0), (0, 0, 0), (0, 0, 0) }
            },
            BlockFace.Back => corner switch
            {
                0 => new[] { (1, -1, 0), (1, 0, 0), (0, -1, 0) },
                1 => new[] { (1, 0, 0), (1, 1, 0), (0, 1, 0) },
                2 => new[] { (0, 1, 0), (-1, 1, 0), (-1, 0, 0) },
                3 => new[] { (-1, 0, 0), (-1, -1, 0), (0, -1, 0) },
                _ => new[] { (0, 0, 0), (0, 0, 0), (0, 0, 0) }
            },
            BlockFace.Left => corner switch
            {
                0 => new[] { (0, -1, -1), (0, -1, 0), (0, 0, -1) },
                1 => new[] { (0, -1, 0), (0, -1, 1), (0, 0, 1) },
                2 => new[] { (0, 0, 1), (0, 1, 1), (0, 1, 0) },
                3 => new[] { (0, 1, 0), (0, 1, -1), (0, 0, -1) },
                _ => new[] { (0, 0, 0), (0, 0, 0), (0, 0, 0) }
            },
            BlockFace.Right => corner switch
            {
                0 => new[] { (0, -1, 1), (0, -1, 0), (0, 0, 1) },
                1 => new[] { (0, -1, 0), (0, -1, -1), (0, 0, -1) },
                2 => new[] { (0, 0, -1), (0, 1, -1), (0, 1, 0) },
                3 => new[] { (0, 1, 0), (0, 1, 1), (0, 0, 1) },
                _ => new[] { (0, 0, 0), (0, 0, 0), (0, 0, 0) }
            },
            _ => new[] { (0, 0, 0), (0, 0, 0), (0, 0, 0) }
        };
    }

    private bool HasOpenSky(ChunkEntity chunk, int x, int y, int z)
    {
        return ColumnHasOpenSky(chunk, x, y, z);
    }

    private bool ColumnHasOpenSky(ChunkEntity chunk, int x, int y, int z)
    {
        // Already at the top of this chunk – assume open sky.
        if (y >= ChunkEntity.Height - 1)
        {
            return true;
        }

        for (int ty = y + 1; ty < ChunkEntity.Height; ty++)
        {
            var blockId = chunk.GetBlock(x, ty, z);

            if (blockId == 0)
            {
                continue;
            }

            var blockType = _blockRegistry.GetById(blockId);

            // Non-solid or explicitly transparent elements (billboards/items/glass) don't block the sky.
            if (!blockType.IsSolid ||
                blockType.IsTransparent ||
                blockType.IsBillboard ||
                blockType.RenderType is BlockRenderType.Item)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private bool HasAdjacentSky(ChunkEntity chunk, int x, int y, int z, BlockFace face)
    {
        Span<(int dx, int dz)> offsets = stackalloc (int, int)[]
        {
            (0, 1), (0, 2), (1, 1), (-1, 1)
        };

        if (face == BlockFace.Back)
        {
            offsets = stackalloc (int, int)[] { (0, -1), (0, -2), (1, -1), (-1, -1) };
        }
        else if (face == BlockFace.Left)
        {
            offsets = stackalloc (int, int)[] { (-1, 0), (-2, 0), (-1, 1), (-1, -1) };
        }
        else if (face == BlockFace.Right)
        {
            offsets = stackalloc (int, int)[] { (1, 0), (2, 0), (1, 1), (1, -1) };
        }

        foreach (var (dx, dz) in offsets)
        {
            int sx = x + dx;
            int sz = z + dz;

            if (!IsWithinChunkBounds(sx, sz) || ColumnHasOpenSky(chunk, sx, y, sz))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWithinChunkBounds(int x, int z)
    {
        return x >= 0 && x < ChunkEntity.Size && z >= 0 && z < ChunkEntity.Size;
    }

    private static bool IsSideFace(BlockFace face)
    {
        return face is BlockFace.Front or BlockFace.Back or BlockFace.Left or BlockFace.Right;
    }

    /// <summary>
    /// Gets the direction index for encoding in vertex alpha channel.
    /// </summary>
    private static byte GetDirectionIndex(BlockFace face) => face switch
    {
        BlockFace.Front => 0,
        BlockFace.Back => 1,
        BlockFace.Right => 2,
        BlockFace.Left => 3,
        BlockFace.Top => 4,
        BlockFace.Bottom => 5,
        _ => 6
    };
}
