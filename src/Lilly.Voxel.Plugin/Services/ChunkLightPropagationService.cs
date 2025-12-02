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
    {
        _blockRegistry = blockRegistry;
    }

    /// <summary>
    /// Calculates lighting for the entire chunk, including sunlight and block emissions.
    /// </summary>
    public void PropagateLight(ChunkEntity chunk)
    {
        // 1. Reset light levels to 0 (darkness) so we can recalculate fresh
        Array.Fill(chunk.LightLevels, (byte)0);

        var lightQueue = new Queue<int>();

        // 2. Initialize Sources (Sunlight & Emissive Blocks)
        InitializeSunlight(chunk, lightQueue);
        InitializeBlockLights(chunk, lightQueue);

        int sourceCount = lightQueue.Count;

        if (sourceCount == 0)
        {
            _logger.Warning(
                "Chunk {Chunk} has NO light sources (Queue is empty)! It will be pitch black.",
                chunk.ChunkCoordinates
            );
        }

        // 3. Propagate (Flood Fill)
        ProcessLightQueue(chunk, lightQueue);

        // 4. Mark clean
        chunk.IsLightingDirty = false;
        chunk.IsMeshDirty = true;
    }

    private void InitializeSunlight(ChunkEntity chunk, Queue<int> lightQueue)
    {
        // Simple vertical sunlight:
        // Iterate every column (X, Z).
        // Go from Top (Y=height-1) down.
        // If block is transparent -> Set to Max Sun.
        // If block blocks light -> Stop (Shadow).

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                bool inShadow = false;

                for (int y = ChunkEntity.Height - 1; y >= 0; y--)
                {
                    int index = ChunkEntity.GetIndex(x, y, z);
                    ushort blockId = chunk.Blocks[index];
                    var blockType = _blockRegistry.GetById(blockId);

                    if (BlocksSunlight(blockType))
                    {
                        inShadow = true;

                        continue; // Sunlight stops here
                    }

                    if (!inShadow)
                    {
                        // Full sunlight
                        chunk.LightLevels[index] = VoxelConstants.MaxLightLevel;

                        // Add to queue so it can spread sideways into caves/overhangs
                        lightQueue.Enqueue(index);
                    }
                }
            }
        }
    }

    private void InitializeBlockLights(ChunkEntity chunk, Queue<int> lightQueue)
    {
        // Scan for blocks that emit light (torches, lava, etc.)
        for (int i = 0; i < chunk.Blocks.Length; i++)
        {
            ushort blockId = chunk.Blocks[i];

            if (blockId == 0)
                continue;

            var blockType = _blockRegistry.GetById(blockId);

            if (blockType.EmitsLight > 0)
            {
                // Map float 0-1 to byte 0-MaxLightLevel
                byte intensity = (byte)(blockType.EmitsLight * VoxelConstants.MaxLightLevel);

                if (intensity > chunk.LightLevels[i])
                {
                    chunk.LightLevels[i] = intensity;

                    // Also set color if applicable
                    if (blockType.EmitsColor != Color4b.Transparent)
                    {
                        chunk.SetLightColor(
                            i % ChunkEntity.Size,
                            (i / ChunkEntity.Size) % ChunkEntity.Height,
                            i / (ChunkEntity.Size * ChunkEntity.Height),
                            blockType.EmitsColor
                        );
                    }

                    lightQueue.Enqueue(i);
                }
            }
        }
    }

    private void ProcessLightQueue(ChunkEntity chunk, Queue<int> lightQueue)
    {
        // Standard BFS Flood Fill
        while (lightQueue.Count > 0)
        {
            int index = lightQueue.Dequeue();

            // Reconstruct coordinates from index
            int x = index % ChunkEntity.Size;
            int y = (index / ChunkEntity.Size) % ChunkEntity.Height;
            int z = index / (ChunkEntity.Size * ChunkEntity.Height);

            byte currentLevel = chunk.LightLevels[index];

            if (currentLevel <= 1)
                continue; // Logic stops at 1, next would be 0

            // Check all 6 neighbors
            CheckNeighbor(chunk, x + 1, y, z, currentLevel, lightQueue);
            CheckNeighbor(chunk, x - 1, y, z, currentLevel, lightQueue);
            CheckNeighbor(chunk, x, y + 1, z, currentLevel, lightQueue);
            CheckNeighbor(chunk, x, y - 1, z, currentLevel, lightQueue);
            CheckNeighbor(chunk, x, y, z + 1, currentLevel, lightQueue);
            CheckNeighbor(chunk, x, y, z - 1, currentLevel, lightQueue);
        }
    }

    private void CheckNeighbor(ChunkEntity chunk, int x, int y, int z, byte currentLevel, Queue<int> lightQueue)
    {
        if (!chunk.IsInBounds(x, y, z))
            return;

        int neighborIndex = ChunkEntity.GetIndex(x, y, z);
        ushort blockId = chunk.Blocks[neighborIndex];
        var blockType = _blockRegistry.GetById(blockId);

        // If block is solid and not transparent (opaque), it blocks light completely.
        if (blockType.IsSolid && !blockType.IsTransparent)
            return;

        // Light decay logic: Air decays by 1, Water might decay by 2 or 3
        int decay = 1;
        if (blockType.RenderType == BlockRenderType.Fluid)
            decay = 2;

        int newLevel = currentLevel - decay;
        if (newLevel < 0)
            newLevel = 0;

        // If we found a brighter path to this neighbor, update it and enqueue
        if (chunk.LightLevels[neighborIndex] < newLevel)
        {
            chunk.LightLevels[neighborIndex] = (byte)newLevel;

            // Propagate color (simplified: copy parent color if neighbor has none or weak)
            // Ideally we mix colors, but simple copy works for now.
            // var parentColor = chunk.GetLightColor(neighborIndex); // actually we need parent coords
            // For now, let's just let the renderer handle the white vs color mix based on intensity

            lightQueue.Enqueue(neighborIndex);
        }
    }

    /// <summary>
    /// Determines whether a block should stop skylight propagation.
    /// Treat any non-transparent, non-billboard/item block as a blocker to avoid
    /// leaks when block definitions forget to mark them as solid.
    /// </summary>
    private static bool BlocksSunlight(BlockType blockType)
    {
        return !blockType.IsTransparent &&
               !blockType.IsBillboard &&
               blockType.RenderType != BlockRenderType.Item;
    }
}
