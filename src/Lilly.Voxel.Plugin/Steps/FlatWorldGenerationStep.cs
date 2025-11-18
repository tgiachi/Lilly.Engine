using Lilly.Engine.Core.Extensions.Strings;
using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;

namespace Lilly.Voxel.Plugin.Steps;

/// <summary>
/// Generates a flat world terrain with layered blocks.
/// Structure from bottom to top:
/// - Level 0: Bedrock (indestructible base)
/// - Levels 1-28: Stone (main underground layer)
/// - Levels 29-31: Dirt (soil layer)
/// - Level 32: Grass (surface layer)
/// - Levels 33+: Air (sky)
/// </summary>
public class FlatWorldGenerationStep : IGeneratorStep
{
    public string Name => "FlatWorldGenerationStep".ToSnakeCase();

    public Task ExecuteAsync(IGeneratorContext context)
    {
        var chunkSize = context.ChunkSize();
        var chunkHeight = context.ChunkHeight();

        // Get block IDs from the block registry
        var bedrockId = context.GetBlockIdByName("bedrock");
        var stoneId = context.GetBlockIdByName("stone");
        var dirtId = context.GetBlockIdByName("dirt");
        var grassId = context.GetBlockIdByName("grass");

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

        // Layer 32: Grass (surface)
        if (grassId.HasValue)
        {
            context.FillLayer(32, grassId.Value);
        }

        return Task.CompletedTask;
    }
}
