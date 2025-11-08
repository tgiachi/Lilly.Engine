using System.Collections.Concurrent;
using System.Diagnostics;
using Lilly.Engine.Core.Interfaces.Jobs;
using Lilly.Engine.Core.Interfaces.Services;
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

        EnqueueJob(new AsyncJobWrapper(job, CancellationToken.None, captureCompletion: false));
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

        var queuedJob = new AsyncJobWrapper(job, cancellationToken, captureCompletion: true);
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
    public void Shutdown()
    {
        _logger.Information("Shutting down job system");
        _cts.Cancel();
    }

    /// <summary>
    /// Releases resources used by the job system service.
    /// </summary>
    public void Dispose()
    {
        _cts.Dispose();
        _queueSignal.Dispose();
        GC.SuppressFinalize(this);
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

    private abstract class QueuedJob
    {
        private readonly CancellationToken _jobCancellationToken;

        protected QueuedJob(string name, CancellationToken cancellationToken)
        {
            Name = name;
            _jobCancellationToken = cancellationToken;
        }

        public string Name { get; }

        public Task ExecuteAsync(CancellationToken serviceToken)
        {
            if (!_jobCancellationToken.CanBeCanceled)
            {
                return ExecuteCoreAsync(serviceToken);
            }

            return ExecuteWithLinkedTokenAsync(serviceToken);
        }

        protected abstract Task ExecuteCoreAsync(CancellationToken cancellationToken);

        private async Task ExecuteWithLinkedTokenAsync(CancellationToken serviceToken)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serviceToken, _jobCancellationToken);
            await ExecuteCoreAsync(linkedCts.Token).ConfigureAwait(false);
        }
    }

    private sealed class SynchronousFireAndForgetJob : QueuedJob
    {
        private readonly IJob _job;

        public SynchronousFireAndForgetJob(IJob job)
            : base(job.Name, CancellationToken.None)
        {
            _job = job;
        }

        protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _job.Execute();
            return Task.CompletedTask;
        }
    }

    private sealed class SynchronousAwaitableJob : QueuedJob
    {
        private readonly IJob _job;
        private readonly TaskCompletionSource<bool> _completionSource;

        public SynchronousAwaitableJob(IJob job, CancellationToken cancellationToken)
            : base(job.Name, cancellationToken)
        {
            _job = job;
            _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task CompletionTask => _completionSource.Task;

        protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _job.Execute();
                _completionSource.TrySetResult(true);
            }
            catch (OperationCanceledException oce)
            {
                var token = oce.CancellationToken.CanBeCanceled ? oce.CancellationToken : cancellationToken;
                _completionSource.TrySetCanceled(token);
                throw;
            }
            catch (Exception ex)
            {
                _completionSource.TrySetException(ex);
                throw;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class SynchronousResultJob<TResult> : QueuedJob
    {
        private readonly IJob<TResult> _job;
        private readonly TaskCompletionSource<TResult> _completionSource;

        public SynchronousResultJob(IJob<TResult> job, CancellationToken cancellationToken)
            : base(job.Name, cancellationToken)
        {
            _job = job;
            _completionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task<TResult> CompletionTask => _completionSource.Task;

        protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = _job.Execute();
                _completionSource.TrySetResult(result);
            }
            catch (OperationCanceledException oce)
            {
                var token = oce.CancellationToken.CanBeCanceled ? oce.CancellationToken : cancellationToken;
                _completionSource.TrySetCanceled(token);
                throw;
            }
            catch (Exception ex)
            {
                _completionSource.TrySetException(ex);
                throw;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class AsyncJobWrapper : QueuedJob
    {
        private readonly IAsyncJob _job;
        private readonly TaskCompletionSource<bool>? _completionSource;

        public AsyncJobWrapper(IAsyncJob job, CancellationToken cancellationToken, bool captureCompletion)
            : base(job.Name, cancellationToken)
        {
            _job = job;
            _completionSource = captureCompletion
                ? new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
                : null;
        }

        public Task CompletionTask => _completionSource?.Task ?? Task.CompletedTask;

        protected override async Task ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _job.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                _completionSource?.TrySetResult(true);
            }
            catch (OperationCanceledException oce)
            {
                var token = oce.CancellationToken.CanBeCanceled ? oce.CancellationToken : cancellationToken;
                _completionSource?.TrySetCanceled(token);
                throw;
            }
            catch (Exception ex)
            {
                _completionSource?.TrySetException(ex);
                throw;
            }
        }
    }

    private sealed class AsyncJobWrapper<TResult> : QueuedJob
    {
        private readonly IAsyncJob<TResult> _job;
        private readonly TaskCompletionSource<TResult> _completionSource;

        public AsyncJobWrapper(IAsyncJob<TResult> job, CancellationToken cancellationToken)
            : base(job.Name, cancellationToken)
        {
            _job = job;
            _completionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task<TResult> CompletionTask => _completionSource.Task;

        protected override async Task ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _job.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                _completionSource.TrySetResult(result);
            }
            catch (OperationCanceledException oce)
            {
                var token = oce.CancellationToken.CanBeCanceled ? oce.CancellationToken : cancellationToken;
                _completionSource.TrySetCanceled(token);
                throw;
            }
            catch (Exception ex)
            {
                _completionSource.TrySetException(ex);
                throw;
            }
        }
    }
}
