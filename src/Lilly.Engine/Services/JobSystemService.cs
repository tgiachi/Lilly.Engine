using System.Collections.Concurrent;
using System.Diagnostics;
using Lilly.Engine.Core.Interfaces.Jobs;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Jobs;
using Serilog;

namespace Lilly.Engine.Services;

/// <summary>
/// Provides a worker-thread job system capable of executing synchronous and asynchronous jobs.
/// </summary>
public class JobSystemService : IJobSystemService, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<JobSystemService>();

    private readonly ConcurrentQueue<QueuedJob> _jobQueue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _queueSignal = new(0);

    /// <summary>
    /// Releases resources used by the job system service.
    /// </summary>
    public void Dispose()
    {
        _cts.Dispose();
        _queueSignal.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public Task ExecuteAsync(IJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var queuedJob = new SynchronousAwaitableJob(job, cancellationToken);
        EnqueueJob(queuedJob);

        return queuedJob.CompletionTask;
    }

    /// <inheritdoc />
    public Task<TResult> ExecuteAsync<TResult>(IJob<TResult> job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var queuedJob = new SynchronousResultJob<TResult>(job, cancellationToken);
        EnqueueJob(queuedJob);

        return queuedJob.CompletionTask;
    }

    /// <inheritdoc />
    public Task ExecuteAsync(IAsyncJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var queuedJob = new AsyncJobWrapper(job, true, cancellationToken);
        EnqueueJob(queuedJob);

        return queuedJob.CompletionTask;
    }

    /// <inheritdoc />
    public Task<TResult> ExecuteAsync<TResult>(IAsyncJob<TResult> job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var queuedJob = new AsyncJobWrapper<TResult>(job, cancellationToken);
        EnqueueJob(queuedJob);

        return queuedJob.CompletionTask;
    }

    /// <inheritdoc />
    public void Initialize(int workerCount)
    {
        _logger.Information("Initializing {count} workers", workerCount);

        for (var i = 0; i < workerCount; i++)
        {
            var thread = new Thread(() => Worker(_cts.Token))
            {
                IsBackground = true,
                Name = $"SquidEngine-JobWorker-{i}"
            };

            thread.Start();
        }
    }

    /// <inheritdoc />
    public void Schedule(IJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        EnqueueJob(new SynchronousFireAndForgetJob(job));
    }

    /// <inheritdoc />
    public void Schedule(IAsyncJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        EnqueueJob(new AsyncJobWrapper(job, false, CancellationToken.None));
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        _logger.Information("Shutting down job system");
        _cts.Cancel();
    }

    private void EnqueueJob(QueuedJob job)
    {
        _jobQueue.Enqueue(job);
        _queueSignal.Release();
    }

    private void Worker(CancellationToken token)
    {
        while (true)
        {
            try
            {
                _queueSignal.Wait(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!token.IsCancellationRequested && _jobQueue.TryDequeue(out var job))
            {
                var stopwatch = Stopwatch.GetTimestamp();

                try
                {
                    job.ExecuteAsync(token).GetAwaiter().GetResult();
                    var elapsed = Stopwatch.GetElapsedTime(stopwatch);
                    _logger.Debug(
                        "Executed job {jobName} in {elapsedMilliseconds} ms from thread: {threadId}",
                        job.Name,
                        elapsed.TotalMilliseconds,
                        Environment.CurrentManagedThreadId
                    );
                }
                catch (OperationCanceledException oce)
                {
                    if (!token.IsCancellationRequested)
                    {
                        var elapsed = Stopwatch.GetElapsedTime(stopwatch);
                        _logger.Debug(
                            "Job {jobName} cancelled after {elapsedMilliseconds} ms on thread {threadId}",
                            job.Name,
                            elapsed.TotalMilliseconds,
                            Environment.CurrentManagedThreadId
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error executing job {jobName} from thread: {threadId}",
                        job.Name,
                        Environment.CurrentManagedThreadId
                    );
                }
            }
        }
    }
}
