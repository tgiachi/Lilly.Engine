using Silk.NET.Maths;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Services;

/// <summary>
/// Provides lighting calculations for chunk mesh generation using ambient occlusion.
/// </summary>
public sealed class ChunkLightingService
{
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

        // Convert to brightness (1.0 = no shadow, 0.0 = full shadow)
        byte brightness = (byte)(averageAO * 255f);

        return new Vector4D<byte>(brightness, brightness, brightness, GetDirectionIndex(face));
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
