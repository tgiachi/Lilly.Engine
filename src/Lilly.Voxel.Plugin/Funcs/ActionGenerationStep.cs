using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Squid.Engine.World.Voxels.Interfaces.Generation.Pipeline;

namespace Lilly.Voxel.Plugin.Funcs;

public class ActionGenerationStep : IGeneratorStep
{
    private readonly Action<IGeneratorContext> _action;
    public string Name { get; }

    public ActionGenerationStep(string name, Action<IGeneratorContext> action)
    {
        Name = name;
        _action = action;
    }

    public Task ExecuteAsync(IGeneratorContext context)
    {
        _action(context);

        return Task.CompletedTask;
    }
}
