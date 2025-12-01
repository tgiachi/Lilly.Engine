using Lilly.Voxel.Plugin.Contexts;
using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Services;

namespace Lilly.Voxel.Plugin.Steps.Lighting;

public class LightingGenerationStep : IGeneratorStep
{
    private readonly ChunkLightPropagationService _lightService;

    public string Name => "Lighting Generation";

    public LightingGenerationStep(ChunkLightPropagationService lightService)
    {
        _lightService = lightService;
    }

    public Task ExecuteAsync(IGeneratorContext context)
    {
        var generatorContext = (GeneratorContext)context;
        // Execute lighting calculation on the chunk
        _lightService.PropagateLight(generatorContext.Chunk);
        return Task.CompletedTask;
    }
}
