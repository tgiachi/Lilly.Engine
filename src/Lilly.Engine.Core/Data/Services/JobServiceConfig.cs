namespace Lilly.Engine.Core.Data.Services;

/// <summary>
/// Configuration for the job service, specifying the number of worker threads.
/// </summary>
/// <param name="WorkerCount">The number of worker threads to use in the job system.</param>
public record JobServiceConfig(int WorkerCount);

