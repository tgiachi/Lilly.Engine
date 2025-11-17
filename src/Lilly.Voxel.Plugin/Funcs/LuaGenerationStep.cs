using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using MoonSharp.Interpreter;
using Squid.Engine.World.Voxels.Interfaces.Generation.Pipeline;

namespace Lilly.Voxel.Plugin.Funcs;

public class LuaGenerationStep : IGeneratorStep
{
    public string Name { get; }

    private readonly Closure _executeFunc;

    public LuaGenerationStep(string name, Closure executeFunc)
    {
        Name = name;
        _executeFunc = executeFunc;
    }

    public Task ExecuteAsync(IGeneratorContext context)
    {
        _executeFunc.Call(context);

        return Task.CompletedTask;
    }
}
