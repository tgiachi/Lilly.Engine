namespace Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;

/// <summary>
/// Defines a single step in the chunk generation pipeline.
/// </summary>
public interface IGeneratorStep
{
    /// <summary>
    /// Gets the name of this generation step.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes this generation step on the provided context.
    /// </summary>
    /// <param name="context">The generation context containing chunk data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(IGeneratorContext context);
}
