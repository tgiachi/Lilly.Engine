using System;
using System.Collections.Generic;
using Lilly.Engine.Core.Extensions.Strings;
using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Steps;

/// <summary>
/// Generates a flat world terrain with layered blocks.
/// Structure from bottom to top:
/// - Level 0: Bedrock (indestructible base)
/// - Levels 1-28: Stone (main underground layer)
/// - Levels 29-31: Dirt (soil layer)
/// - Level 31: Grass (surface layer)
/// - Levels 32+: Air and decorative elements (flowers)
/// </summary>
public class FlatWorldGenerationStep : IGeneratorStep
{
    public string Name => "FlatWorldGenerationStep".ToSnakeCase();

    public Task ExecuteAsync(IGeneratorContext context)
    {
        var chunkSize = context.ChunkSize();
        var chunkHeight = context.ChunkHeight();
        var chunkBaseY = (int)context.WorldPosition.Y;

        if (chunkBaseY >= ChunkEntity.Height)
        {
            context.FillBlocks(0, 0, 0, chunkSize, chunkHeight, chunkSize, 0);

            return Task.CompletedTask;
        }

        // Get block IDs from the block registry
        var bedrockId = context.GetBlockIdByName("bedrock");
        var stoneId = context.GetBlockIdByName("stone");
        var dirtId = context.GetBlockIdByName("dirt");
        var grassId = context.GetBlockIdByName("grass");
        var flowerIds = GetFlowerIds(context);
        var itemId = context.GetBlockIdByName("item_1");

        // Layer 0: Bedrock (indestructible foundation)
        if (bedrockId.HasValue)
        {
            context.FillLayer(0, bedrockId.Value);
        }

        // Layers 1-28: Stone (main underground)
        if (stoneId.HasValue)
        {
            context.FillBlocks(0, 1, 0, chunkSize, 29, chunkSize, stoneId.Value);
        }

        // Layers 29-31: Dirt (soil layer)
        if (dirtId.HasValue)
        {
            context.FillBlocks(0, 29, 0, chunkSize, 32, chunkSize, dirtId.Value);
        }

        // Layer 31: Grass (surface)
        if (grassId.HasValue)
        {
            context.FillLayer(31, grassId.Value);
        }

        PlaceFlowers(context, grassId, flowerIds);
        PlaceItem(context, grassId, itemId);

        return Task.CompletedTask;
    }

    private static List<ushort> GetFlowerIds(IGeneratorContext context)
    {
        var ids = new List<ushort>();

        for (int i = 1; i <= 8; i++)
        {
            var id = context.GetBlockIdByName($"flowers{i}");

            if (id.HasValue)
            {
                ids.Add(id.Value);
            }
        }

        return ids;
    }

    private static void PlaceFlowers(IGeneratorContext context, ushort? grassId, List<ushort> flowerIds)
    {
        if (!grassId.HasValue || flowerIds.Count == 0 || context.ChunkHeight() <= 32)
        {
            return;
        }

        var chunk = context.GetChunk();
        var coordinates = chunk.ChunkCoordinates;
        var randomSeed = HashCode.Combine(context.Seed, coordinates.X, coordinates.Y, coordinates.Z);
        var random = new Random(randomSeed);

        const int flowersPerChunk = 2;

        for (int i = 0; i < flowersPerChunk; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < 16 && !placed; attempt++)
            {
                int x = random.Next(0, context.ChunkSize());
                int z = random.Next(0, context.ChunkSize());

                if (chunk.GetBlock(x, 31, z) != grassId.Value)
                {
                    continue;
                }

                var flowerId = flowerIds[random.Next(flowerIds.Count)];
                context.SetBlock(x, 32, z, flowerId);
                placed = true;
            }
        }
    }

    private static void PlaceItem(IGeneratorContext context, ushort? grassId, ushort? itemId)
    {
        if (!grassId.HasValue || !itemId.HasValue || context.ChunkHeight() <= 32)
        {
            return;
        }

        var chunk = context.GetChunk();
        var coordinates = chunk.ChunkCoordinates;
        var randomSeed = HashCode.Combine(context.Seed, coordinates.X, coordinates.Y, coordinates.Z, 1337);
        var random = new Random(randomSeed);

        for (int attempt = 0; attempt < 16; attempt++)
        {
            int x = random.Next(0, context.ChunkSize());
            int z = random.Next(0, context.ChunkSize());

            if (chunk.GetBlock(x, 31, z) != grassId.Value)
            {
                continue;
            }

            if (chunk.GetBlock(x, 32, z) != 0)
            {
                continue;
            }

            context.SetBlock(x, 32, z, itemId.Value);
            break;
        }
    }
}
