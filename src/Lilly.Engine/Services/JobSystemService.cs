using System.Collections.Concurrent;
using System.Diagnostics;
using Lilly.Engine.Core.Interfaces.Jobs;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Jobs;
using Serilog;

namespace Lilly.Engine.Services;

/// <summary>
/// Provides a worker-thread job system capable of executing synchronous and asynchronous jobs with priority support.
/// </summary>
public class JobSystemService : IJobSystemService, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<JobSystemService>();

    // Thread-safe priority queue for job scheduling
    private readonly PriorityQueue<QueuedJob, QueuedJob> _jobQueue = new();
    private readonly ReaderWriterLockSlim _queueLock = new();

    // Signaling
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _queueSignal = new(0);

    // Metrics tracking
    private long _totalJobsExecuted;
    private long _cancelledJobsCount;
    private long _failedJobsCount;
    private double _totalExecutionTimeMs;
    private double _minExecutionTimeMs = double.MaxValue;
    private double _maxExecutionTimeMs;
    private int _activeWorkerCount;
    private int _totalWorkerCount;
    private readonly Lock _metricsLock = new();
    private bool _disposed;

    /// <summary>
    /// Releases resources used by the job system service.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
        _queueSignal.Dispose();
        _queueLock.Dispose();
        GC.SuppressFinalize(this);
    }

#region Metrics Implementation

    public int PendingJobCount
    {
        get
        {
            _queueLock.EnterReadLock();

            try
            {
                return _jobQueue.Count;
            }
            finally
            {
                _queueLock.ExitReadLock();
            }
        }
    }

    public long TotalJobsExecuted => Interlocked.Read(ref _totalJobsExecuted);
    public int ActiveWorkerCount => Interlocked.CompareExchange(ref _activeWorkerCount, 0, 0);
    public int TotalWorkerCount => Interlocked.CompareExchange(ref _totalWorkerCount, 0, 0);
    public long CancelledJobsCount => Interlocked.Read(ref _cancelledJobsCount);
    public long FailedJobsCount => Interlocked.Read(ref _failedJobsCount);

    public double AverageExecutionTimeMs
    {
        get
        {
            var executed = Interlocked.Read(ref _totalJobsExecuted);

            if (executed == 0)
                return 0;

            lock (_metricsLock)
            {
                return _totalExecutionTimeMs / executed;
            }
        }
    }

    public double MinExecutionTimeMs => _minExecutionTimeMs;
    public double MaxExecutionTimeMs => _maxExecutionTimeMs;

#endregion

#region IJob Execution

    public IJobHandle ExecuteAsync(
        IJob job,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(job);

        var queuedJob = new SynchronousAwaitableJob(job, priority, cancellationToken);
        var handle = new JobHandle(queuedJob);
        EnqueueJobWithCompletion(queuedJob, handle);

        return handle;
    }

    public IJobHandle<TResult> ExecuteAsync<TResult>(
        IJob<TResult> job,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(job);

        var queuedJob = new SynchronousResultJob<TResult>(job, priority, cancellationToken);
        var handle = new JobHandle<TResult>(queuedJob);
        EnqueueJobWithCompletion(queuedJob, handle);

        return handle;
    }

#endregion

#region IAsyncJob Execution

    public IJobHandle ExecuteAsync(
        IAsyncJob job,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(job);

        var queuedJob = new AsyncJobWrapper(job, priority, true, cancellationToken);
        var handle = new JobHandle(queuedJob);
        EnqueueJobWithCompletion(queuedJob, handle);

        return handle;
    }

    public IJobHandle<TResult> ExecuteAsync<TResult>(
        IAsyncJob<TResult> job,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(job);

        var queuedJob = new AsyncJobWrapper<TResult>(job, priority, cancellationToken);
        var handle = new JobHandle<TResult>(queuedJob);
        EnqueueJobWithCompletion(queuedJob, handle);

        return handle;
    }

#endregion

#region Task Factory Execution

    public IJobHandle<TResult> ExecuteTaskAsync<TResult>(
        string name,
        Func<CancellationToken, Task<TResult>> taskFactory,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(taskFactory);

        var queuedJob = new TaskResultJob<TResult>(name, taskFactory, priority, cancellationToken);
        var handle = new JobHandle<TResult>(queuedJob);
        EnqueueJobWithCompletion(queuedJob, handle);

        return handle;
    }

    public IJobHandle<TResult> ExecuteTaskAsync<TResult>(
        string name,
        Func<Task<TResult>> taskFactory,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(taskFactory);

        var queuedJob = new TaskResultJob<TResult>(name, taskFactory, priority, cancellationToken);
        var handle = new JobHandle<TResult>(queuedJob);
        EnqueueJobWithCompletion(queuedJob, handle);

        return handle;
    }

#endregion

#region Lambda Execution (Action & Func)

    public IJobHandle ExecuteAsync(
        string name,
        Action action,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(action);

        var job = new ActionJob(name, action);
        var queuedJob = new SynchronousAwaitableJob(job, priority, cancellationToken);
        var handle = new JobHandle(queuedJob);
        EnqueueJobWithCompletion(queuedJob, handle);

        return handle;
    }

    public IJobHandle<TResult> ExecuteAsync<TResult>(
        string name,
        Func<TResult> action,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(action);

        var job = new FuncJob<TResult>(name, action);
        var queuedJob = new SynchronousResultJob<TResult>(job, priority, cancellationToken);
        var handle = new JobHandle<TResult>(queuedJob);
        EnqueueJobWithCompletion(queuedJob, handle);

        return handle;
    }

    public IJobHandle ExecuteAsync(
        string name,
        Func<Task> asyncAction,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(asyncAction);

        var job = new AsyncActionJob(name, asyncAction);
        var queuedJob = new AsyncJobWrapper(job, priority, true, cancellationToken);
        var handle = new JobHandle(queuedJob);
        EnqueueJobWithCompletion(queuedJob, handle);

        return handle;
    }

    public IJobHandle<TResult> ExecuteAsync<TResult>(
        string name,
        Func<Task<TResult>> asyncAction,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(asyncAction);

        var job = new AsyncFuncJob<TResult>(name, asyncAction);
        var queuedJob = new AsyncJobWrapper<TResult>(job, priority, cancellationToken);
        var handle = new JobHandle<TResult>(queuedJob);
        EnqueueJobWithCompletion(queuedJob, handle);

        return handle;
    }

#endregion

#region Fire-and-Forget Scheduling

    public IJobHandle Schedule(IJob job, JobPriority priority = JobPriority.Normal)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(job);

        var queuedJob = new SynchronousFireAndForgetJob(job, priority);
        var handle = new JobHandle(queuedJob);
        EnqueueJobWithoutCompletion(queuedJob);

        return handle;
    }

    public IJobHandle Schedule(
        IAsyncJob job,
        JobPriority priority = JobPriority.Normal,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(job);

        var queuedJob = new AsyncJobWrapper(job, priority, false, cancellationToken);
        var handle = new JobHandle(queuedJob);
        EnqueueJobWithoutCompletion(queuedJob);

        return handle;
    }

#endregion

#region Lifecycle

    public void Initialize(int workerCount)
    {
        ThrowIfDisposed();
        _logger.Information("Initializing {count} workers", workerCount);

        Interlocked.Exchange(ref _totalWorkerCount, workerCount);

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

    public void Shutdown()
    {
        if (_disposed)
            return;

        _logger.Information("Shutting down job system");
        _cts.Cancel();
    }

#endregion

#region Private Methods

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(JobSystemService));
    }

    private void EnqueueJobWithCompletion<THandle>(QueuedJob job, THandle handle)
        where THandle : class
    {
        _queueLock.EnterWriteLock();

        try
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JobSystemService));
            }

            _jobQueue.Enqueue(job, job);
            _queueSignal.Release();
        }
        finally
        {
            _queueLock.ExitWriteLock();
        }
    }

    private void EnqueueJobWithoutCompletion(QueuedJob job)
    {
        _queueLock.EnterWriteLock();

        try
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JobSystemService));
            }

            _jobQueue.Enqueue(job, job);
            _queueSignal.Release();
        }
        finally
        {
            _queueLock.ExitWriteLock();
        }
    }

    private void Worker(CancellationToken token)
    {
        Interlocked.Increment(ref _activeWorkerCount);

        try
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

                while (!token.IsCancellationRequested && TryDequeueJob(out var job))
                {
                    var stopwatch = Stopwatch.GetTimestamp();

                    try
                    {
                        job.ExecuteAsync(token).GetAwaiter().GetResult();
                        var elapsed = Stopwatch.GetElapsedTime(stopwatch);
                        UpdateMetrics(elapsed, isCancelled: false, isFailed: false);

                        _logger.Debug(
                            "Executed job {jobName} (priority: {priority}) in {elapsedMilliseconds} ms from thread: {threadId}",
                            job.Name,
                            job.Priority,
                            elapsed.TotalMilliseconds,
                            Environment.CurrentManagedThreadId
                        );
                    }
                    catch (OperationCanceledException oce)
                    {
                        var elapsed = Stopwatch.GetElapsedTime(stopwatch);
                        Interlocked.Increment(ref _cancelledJobsCount);

                        if (!token.IsCancellationRequested)
                        {
                            _logger.Debug(
                                "Job {jobName} (priority: {priority}) cancelled after {elapsedMilliseconds} ms on thread {threadId}",
                                job.Name,
                                job.Priority,
                                elapsed.TotalMilliseconds,
                                Environment.CurrentManagedThreadId
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        var elapsed = Stopwatch.GetElapsedTime(stopwatch);
                        Interlocked.Increment(ref _failedJobsCount);

                        _logger.Error(
                            ex,
                            "Error executing job {jobName} (priority: {priority}) from thread: {threadId}",
                            job.Name,
                            job.Priority,
                            Environment.CurrentManagedThreadId
                        );
                    }
                    finally
                    {
                        job.Dispose();
                    }
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref _activeWorkerCount);
        }
    }

    private bool TryDequeueJob(out QueuedJob job)
    {
        _queueLock.EnterReadLock();

        try
        {
            return _jobQueue.TryDequeue(out job, out _);
        }
        finally
        {
            _queueLock.ExitReadLock();
        }
    }

    private void UpdateMetrics(TimeSpan elapsed, bool isCancelled, bool isFailed)
    {
        Interlocked.Increment(ref _totalJobsExecuted);

        var elapsedMs = elapsed.TotalMilliseconds;

        lock (_metricsLock)
        {
            _totalExecutionTimeMs += elapsedMs;

            if (elapsedMs < _minExecutionTimeMs)
                _minExecutionTimeMs = elapsedMs;

            if (elapsedMs > _maxExecutionTimeMs)
                _maxExecutionTimeMs = elapsedMs;
        }
    }

#endregion
}

#region Helper Job Classes for Lambda Support

/// <summary>Helper job class that wraps an Action.</summary>
internal sealed class ActionJob : IJob
{
    private readonly Action _action;
    public string Name { get; }

    public ActionJob(string name, Action action)
    {
        Name = name;
        _action = action;
    }

    public void Execute()
        => _action();
}

/// <summary>Helper job class that wraps a Func{T}.</summary>
internal sealed class FuncJob<TResult> : IJob<TResult>
{
    private readonly Func<TResult> _func;
    public string Name { get; }

    public FuncJob(string name, Func<TResult> func)
    {
        Name = name;
        _func = func;
    }

    public TResult Execute()
        => _func();
}

/// <summary>Helper job class that wraps a Func{Task}.</summary>
internal sealed class AsyncActionJob : IAsyncJob
{
    private readonly Func<Task> _asyncAction;
    public string Name { get; }

    public AsyncActionJob(string name, Func<Task> asyncAction)
    {
        Name = name;
        _asyncAction = asyncAction;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
        => _asyncAction();
}

/// <summary>Helper job class that wraps a Func{Task{T}}.</summary>
internal sealed class AsyncFuncJob<TResult> : IAsyncJob<TResult>
{
    private readonly Func<Task<TResult>> _asyncFunc;
    public string Name { get; }

    public AsyncFuncJob(string name, Func<Task<TResult>> asyncFunc)
    {
        Name = name;
        _asyncFunc = asyncFunc;
    }

    public Task<TResult> ExecuteAsync(CancellationToken cancellationToken)
        => _asyncFunc();
}

#endregion
