# Job System

The Job System in Lilly.Engine allows you to execute tasks asynchronously across multiple worker threads. This is crucial for maintaining a high frame rate by moving expensive operations (like terrain generation, pathfinding, or asset loading) off the main thread.

## Overview

The `IJobSystemService` manages a pool of worker threads. You schedule "jobs" with a priority, and the system executes them as threads become available.

- **Multithreaded**: Uses `System.Threading` and `Task` under the hood.
- **Prioritized**: Critical jobs run before background tasks.
- **Safe**: Handles cancellation and exception logging.

## Core Concepts

- **Job**: A unit of work (Action or Func).
- **Priority**: Determines execution order (`Critical`, `High`, `Normal`, `Low`).
- **Handle**: An object returned when scheduling a job, used to await completion or check status.

## Basic Usage

Inject `IJobSystemService` into your class:

```csharp
public class TerrainManager
{
    private readonly IJobSystemService _jobSystem;

    public TerrainManager(IJobSystemService jobSystem)
    {
        _jobSystem = jobSystem;
    }
}
```

### Fire and Forget

Run a task in the background without waiting for it:

```csharp
_jobSystem.Schedule("CleanUpCache", (token) =>
{
    // Heavy work here
    Cache.Clean();
    return Task.CompletedTask;
}, JobPriority.Low);
```

### Waiting for Results

Schedule a job and await its result:

```csharp
public async Task GenerateChunkAsync()
{
    // Schedule calculation
    var handle = _jobSystem.Schedule<ChunkData>("GenerateChunk", async (token) => 
    {
        var data = new ChunkData();
        // Expensive math...
        await Task.Delay(100); 
        return data;
    });

    // Wait for it to finish
    ChunkData result = await handle.Task;
    
    // Use result
    ApplyChunk(result);
}
```

### Using Callbacks

Alternatively, provide a callback to run when the job finishes:

```csharp
_jobSystem.Schedule(
    "Pathfinding",
    (token) => FindPath(start, end),
    JobPriority.Normal,
    onComplete: (path) => 
    {
        // This runs on a worker thread! 
        // Be careful touching main-thread-only objects here.
        _npc.SetPath(path);
    }
);
```

**Important:** The `onComplete` callback usually runs on the worker thread. If you need to update the UI or modify scene objects that aren't thread-safe, use `JobSystem.RunInMainThread` (if available via your dispatcher) or `Scene.EnqueueAction`.

## Priorities

Use priorities to ensure the game stays responsive:

- **Critical**: Immediate game logic requirements (e.g., physics for the current frame).
- **High**: Important but not blocking frame render (e.g., loading the next room).
- **Normal**: Standard background tasks (e.g., AI decision making).
- **Low**: Maintenance tasks (e.g., garbage collection, cleanup).

## Monitoring

The engine includes a built-in Job System Debugger (Toggle with **F2**).

- **Pending Jobs**: Number of jobs waiting in the queue.
- **Active Workers**: How many threads are currently busy.
- **Execution Time**: Min/Max/Avg time jobs are taking.
- **History**: List of recently completed or failed jobs.

## Best Practices

1.  **Avoid Shared State**: Jobs should ideally work on isolated data structures. If you must share data, use locks or thread-safe collections (`ConcurrentQueue`, etc.).
2.  **Check Cancellation**: Respect the `CancellationToken` passed to your job action.
    ```csharp
    _jobSystem.Schedule("LongTask", async (token) =>
    {
        foreach (var item in items)
        {
            if (token.IsCancellationRequested) break;
            Process(item);
        }
    });
    ```
3.  **Don't Touch GL/UI**: Never call rendering commands or modify UI objects directly from a job. Compute the data in the job, then apply it on the main thread.
