using System.Buffers;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Data;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Types;
using Serilog;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Services;

/// <summary>
/// Handles the propagation of light (sunlight and block light) within a chunk.
/// Uses a Flood-Fill (BFS) algorithm to spread light from sources to neighboring blocks.
/// </summary>
public sealed class ChunkLightPropagationService
{
    private readonly IBlockRegistry _blockRegistry;
    private readonly ILogger _logger = Log.ForContext<ChunkLightPropagationService>();

    public ChunkLightPropagationService(IBlockRegistry blockRegistry)
        => _blockRegistry = blockRegistry;

    /// <summary>
    /// Calculates lighting for the entire chunk, including sunlight and block emissions.
    /// </summary>
    public void PropagateLight(ChunkEntity chunk, ChunkEntity? topNeighbor = null)
    {
        // 1. Rent buffer from ArrayPool to reduce GC pressure
        var bufferSize = chunk.LightLevels.Length;
        var lightBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        try
        {
            // Reset buffer (ArrayPool arrays are not guaranteed to be clean)
            Array.Clear(lightBuffer, 0, bufferSize);

            var lightQueue = new Queue<int>();

            // 2. Initialize Sources (Sunlight & Emissive Blocks)
            InitializeSunlight(chunk, topNeighbor, lightQueue, lightBuffer);
            InitializeBlockLights(chunk, lightQueue, lightBuffer);

            var sourceCount = lightQueue.Count;

            if (sourceCount == 0)
            {
                // Only warn if we really expect light. Underground chunks might legally be dark.
                // _logger.Warning("..."); 
            }

            // 3. Propagate (Flood Fill)
            ProcessLightQueue(chunk, lightQueue, lightBuffer);

            // 4. Swap atomically
            // We must copy the data from the pooled array to a new permanent array for the chunk,
            // because we are returning the pooled array immediately.
            // (Alternatively, ChunkEntity could own the rented array, but that complicates lifecycle management).
            // For now, copying is safer and still saves the massive allocation of the temporary buffer if we did "new byte[]" every time.

            var finalLevels = new byte[bufferSize];
            Array.Copy(lightBuffer, finalLevels, bufferSize);

            chunk.ReplaceLightLevels(finalLevels);

            // 5. Mark clean
            chunk.IsLightingDirty = false;
            chunk.IsMeshDirty = true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(lightBuffer);
        }
    }

    /// <summary>
    /// Determines whether a block should stop skylight propagation.
    /// Treat any non-transparent, non-billboard/item block as a blocker to avoid
    /// leaks when block definitions forget to mark them as solid.
    /// </summary>
    private static bool BlocksSunlight(BlockType blockType)
        => !blockType.IsTransparent &&
           !blockType.IsBillboard &&
           blockType.RenderType != BlockRenderType.Item;

    private void CheckNeighbor(
        ChunkEntity chunk,
        int x,
        int y,
        int z,
        byte currentLevel,
        Queue<int> lightQueue,
        byte[] lightLevels
    )
    {
        if (!chunk.IsInBounds(x, y, z))
        {
            return;
        }

        var neighborIndex = ChunkEntity.GetIndex(x, y, z);
        var blockId = chunk.Blocks[neighborIndex];
        var blockType = _blockRegistry.GetById(blockId);

        // If block is solid and not transparent (opaque), it blocks light completely.
        if (blockType.IsSolid && !blockType.IsTransparent)
        {
            return;
        }

        // Light decay logic: Air decays by 1, Water might decay by 2 or 3
        var decay = 1;

        if (blockType.RenderType == BlockRenderType.Fluid)
        {
            decay = 2;
        }

        var newLevel = currentLevel - decay;

        if (newLevel < 0)
        {
            newLevel = 0;
        }

        // If we found a brighter path to this neighbor, update it and enqueue
        if (lightLevels[neighborIndex] < newLevel)
        {
            lightLevels[neighborIndex] = (byte)newLevel;

            // Propagate color (simplified: copy parent color if neighbor has none or weak)
            // Ideally we mix colors, but simple copy works for now.
            // var parentColor = chunk.GetLightColor(neighborIndex); // actually we need parent coords
            // For now, let's just let the renderer handle the white vs color mix based on intensity

            lightQueue.Enqueue(neighborIndex);
        }
    }

    private void InitializeBlockLights(ChunkEntity chunk, Queue<int> lightQueue, byte[] lightLevels)
    {
        // Scan for blocks that emit light (torches, lava, etc.)
        for (var i = 0; i < chunk.Blocks.Length; i++)
        {
            var blockId = chunk.Blocks[i];

            if (blockId == 0)
            {
                continue;
            }

            var blockType = _blockRegistry.GetById(blockId);

            if (blockType.EmitsLight > 0)
            {
                // Map float 0-1 to byte 0-MaxLightLevel
                var intensity = (byte)(blockType.EmitsLight * VoxelConstants.MaxLightLevel);

                if (intensity > lightLevels[i])
                {
                    lightLevels[i] = intensity;

                    // Also set color if applicable
                    if (blockType.EmitsColor != Color4b.Transparent)
                    {
                        chunk.SetLightColor(
                            i % ChunkEntity.Size,
                            i / ChunkEntity.Size % ChunkEntity.Height,
                            i / (ChunkEntity.Size * ChunkEntity.Height),
                            blockType.EmitsColor
                        );
                    }

                    lightQueue.Enqueue(i);
                }
            }
        }
    }

    private void InitializeSunlight(ChunkEntity chunk, ChunkEntity? topNeighbor, Queue<int> lightQueue, byte[] lightLevels)
    {
        // Simple vertical sunlight:
        // Iterate every column (X, Z).
        // Go from Top (Y=height-1) down.
        // If block is transparent -> Set to Max Sun.
        // If block blocks light -> Stop (Shadow).

        for (var x = 0; x < ChunkEntity.Size; x++)
        {
            for (var z = 0; z < ChunkEntity.Size; z++)
            {
                var inShadow = false;

                // If we have a chunk above, check if it allows sunlight through
                if (topNeighbor != null)
                {
                    // Check the light level at the bottom of the chunk above
                    if (topNeighbor.GetLightLevel(x, 0, z) < VoxelConstants.MaxLightLevel)
                    {
                        inShadow = true;
                    }
                }

                for (var y = ChunkEntity.Height - 1; y >= 0; y--)
                {
                    var index = ChunkEntity.GetIndex(x, y, z);
                    var blockId = chunk.Blocks[index];
                    var blockType = _blockRegistry.GetById(blockId);

                    if (BlocksSunlight(blockType))
                    {
                        inShadow = true;

                        continue; // Sunlight stops here
                    }

                    if (!inShadow)
                    {
                        // Full sunlight
                        lightLevels[index] = VoxelConstants.MaxLightLevel;

                        // Add to queue so it can spread sideways into caves/overhangs
                        lightQueue.Enqueue(index);
                    }
                }
            }
        }
    }

    private void ProcessLightQueue(ChunkEntity chunk, Queue<int> lightQueue, byte[] lightLevels)
    {
        // Standard BFS Flood Fill
        while (lightQueue.Count > 0)
        {
            var index = lightQueue.Dequeue();

            // Reconstruct coordinates from index
            var x = index % ChunkEntity.Size;
            var y = index / ChunkEntity.Size % ChunkEntity.Height;
            var z = index / (ChunkEntity.Size * ChunkEntity.Height);

            var currentLevel = lightLevels[index];

            if (currentLevel <= 1)
            {
                continue; // Logic stops at 1, next would be 0
            }

            // Check all 6 neighbors
            CheckNeighbor(chunk, x + 1, y, z, currentLevel, lightQueue, lightLevels);
            CheckNeighbor(chunk, x - 1, y, z, currentLevel, lightQueue, lightLevels);
            CheckNeighbor(chunk, x, y + 1, z, currentLevel, lightQueue, lightLevels);
            CheckNeighbor(chunk, x, y - 1, z, currentLevel, lightQueue, lightLevels);
            CheckNeighbor(chunk, x, y, z + 1, currentLevel, lightQueue, lightLevels);
            CheckNeighbor(chunk, x, y, z - 1, currentLevel, lightQueue, lightLevels);
        }
    }
}
