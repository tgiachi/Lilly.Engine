using System.Numerics;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Services;

/// <summary>
/// Builds lightweight collision data for a chunk by fusing solid blocks into coarse sub-chunk boxes.
/// </summary>
public sealed class ChunkColliderBuilder
{
    private readonly IBlockRegistry _blockRegistry;

    // Tuneable granularity: smaller values increase accuracy but create more boxes.
    private const int CellSize = 8;

    public ChunkColliderBuilder(IBlockRegistry blockRegistry)
    {
        _blockRegistry = blockRegistry;
    }

    public ChunkColliderData Build(ChunkEntity chunk)
    {
        var colliderData = new ChunkColliderData();

        if (chunk.BlockCount == 0)
        {
            return colliderData;
        }

        for (int sx = 0; sx < ChunkEntity.Size; sx += CellSize)
        {
            for (int sy = 0; sy < ChunkEntity.Height; sy += CellSize)
            {
                for (int sz = 0; sz < ChunkEntity.Size; sz += CellSize)
                {
                    if (!HasSolidBlocks(chunk, sx, sy, sz))
                    {
                        continue;
                    }

                    var min = new Vector3(sx, sy, sz);
                    var max = new Vector3(
                        MathF.Min(sx + CellSize, ChunkEntity.Size),
                        MathF.Min(sy + CellSize, ChunkEntity.Height),
                        MathF.Min(sz + CellSize, ChunkEntity.Size)
                    );

                    colliderData.Boxes.Add(new ChunkColliderBox(min, max));
                }
            }
        }

        return colliderData;
    }

    private bool HasSolidBlocks(ChunkEntity chunk, int startX, int startY, int startZ)
    {
        var endX = Math.Min(startX + CellSize, ChunkEntity.Size);
        var endY = Math.Min(startY + CellSize, ChunkEntity.Height);
        var endZ = Math.Min(startZ + CellSize, ChunkEntity.Size);

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                for (int z = startZ; z < endZ; z++)
                {
                    var blockId = chunk.GetBlockFast(x, y, z);

                    if (blockId == 0)
                    {
                        continue;
                    }

                    var blockType = _blockRegistry.GetById(blockId);

                    if (blockType.IsSolid && !blockType.IsLiquid)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
