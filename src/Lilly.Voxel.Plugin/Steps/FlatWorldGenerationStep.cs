using Lilly.Engine.Core.Extensions.Strings;
using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;

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
        var chunkWorldY = (int)context.WorldPosition.Y;

        // Define global surface height (World Y)
        const int SurfaceY = 15;
        const int DirtDepth = 3;

        // Get block IDs
        var bedrockId = context.GetBlockIdByName("bedrock");
        var stoneId = context.GetBlockIdByName("stone");
        var dirtId = context.GetBlockIdByName("dirt");
        var grassId = context.GetBlockIdByName("grass");
        var flowerIds = GetFlowerIds(context);
        var itemId = context.GetBlockIdByName("item_1");

        // Iterate through each vertical layer of the chunk
        for (var y = 0; y < chunkHeight; y++)
        {
            var currentWorldY = chunkWorldY + y;

            // Don't generate anything below world 0 (void)
            if (currentWorldY < 0)
            {
                continue;
            }

            ushort? blockToPlace = null;

            if (currentWorldY == 0)
            {
                // Bottom of the world
                blockToPlace = bedrockId;
            }
            else if (currentWorldY < SurfaceY - DirtDepth)
            {
                // Deep underground
                blockToPlace = stoneId;
            }
            else if (currentWorldY < SurfaceY)
            {
                // Dirt layer just below surface
                blockToPlace = dirtId;
            }
            else if (currentWorldY == SurfaceY)
            {
                // Surface
                blockToPlace = grassId;
            }

            if (blockToPlace.HasValue)
            {
                context.FillLayer(y, blockToPlace.Value);
            }
        }

        // Decorators (Flowers & Items) - placed at SurfaceY + 1
        // We only place them if this chunk contains the layer just above the surface
        var decorationWorldY = SurfaceY + 1;
        var localDecorationY = decorationWorldY - chunkWorldY;

        if (localDecorationY >= 0 && localDecorationY < chunkHeight && grassId.HasValue)
        {
            PlaceFlowers(context, localDecorationY, grassId.Value, flowerIds);
            PlaceItem(context, localDecorationY, grassId.Value, itemId);
        }

        return Task.CompletedTask;
    }

    private static List<ushort> GetFlowerIds(IGeneratorContext context)
    {
        var ids = new List<ushort>();

        for (var i = 1; i <= 8; i++)
        {
            var id = context.GetBlockIdByName($"flowers{i}");

            if (id.HasValue)
            {
                ids.Add(id.Value);
            }
        }

        return ids;
    }

    private static void PlaceFlowers(IGeneratorContext context, int y, ushort grassId, List<ushort> flowerIds)
    {
        if (flowerIds.Count == 0)
        {
            return;
        }

        var chunk = context.GetChunk();
        var coordinates = chunk.ChunkCoordinates;
        var randomSeed = HashCode.Combine(context.Seed, coordinates.X, coordinates.Y, coordinates.Z);
        var random = new Random(randomSeed);

        const double flowerChance = 0.002; // 0.2% per grass column

        for (var z = 0; z < context.ChunkSize(); z++)
        {
            for (var x = 0; x < context.ChunkSize(); x++)
            {
                if (random.NextDouble() > flowerChance)
                {
                    continue;
                }

                // Check block below (y-1) to ensure it's grass
                // Note: We need to be careful if y=0, checking y-1 would be out of bounds for this chunk.
                // However, since we are strictly placing at SurfaceY+1, and SurfaceY is usually grass, 
                // we can assume valid placement if the logic above is correct.
                // But for safety/correctness, we should check the actual block if it's within bounds.

                if (y > 0 && chunk.GetBlock(x, y - 1, z) != grassId)
                {
                    continue;
                }

                var flowerId = flowerIds[random.Next(flowerIds.Count)];
                context.SetBlock(x, y, z, flowerId);
            }
        }
    }

    private static void PlaceItem(IGeneratorContext context, int y, ushort grassId, ushort? itemId)
    {
        if (!itemId.HasValue)
        {
            return;
        }

        var chunk = context.GetChunk();
        var coordinates = chunk.ChunkCoordinates;
        var randomSeed = HashCode.Combine(context.Seed, coordinates.X, coordinates.Y, coordinates.Z, 1337);
        var random = new Random(randomSeed);

        for (var attempt = 0; attempt < 16; attempt++)
        {
            var x = random.Next(0, context.ChunkSize());
            var z = random.Next(0, context.ChunkSize());

            if (y > 0 && chunk.GetBlock(x, y - 1, z) != grassId)
            {
                continue;
            }

            if (chunk.GetBlock(x, y, z) != 0)
            {
                continue;
            }

            context.SetBlock(x, y, z, itemId.Value);

            break;
        }
    }
}
