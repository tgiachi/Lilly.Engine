using System.Diagnostics;
using Lilly.Engine.Core.Data.Services;
using Lilly.Engine.Core.Interfaces.Jobs;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Jobs;
using Serilog;

namespace Lilly.Engine.Services;

/// <summary>
/// Provides a worker-thread job system capable of executing jobs with priority support.
/// </summary>
public class JobSystemService : IJobSystemService, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<JobSystemService>();
    private readonly JobServiceConfig _config;

    // Priority queue for job scheduling (higher priority values come first)
    private readonly PriorityQueue<ScheduledJob, JobPriority> _jobQueue =
        new(Comparer<JobPriority>.Create((x, y) => y.CompareTo(x)));

    private readonly Lock _queueLock = new();

    // Signaling
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _queueSignal = new(0);

    // Metrics
    private long _totalJobsExecuted;
    private long _cancelledJobsCount;
    private long _failedJobsCount;
    private double _totalExecutionTimeMs;
    private double _minExecutionTimeMs = double.MaxValue;
    private double _maxExecutionTimeMs;
    private int _activeWorkerCount;
    private int _totalWorkerCount;
    private readonly Lock _metricsLock = new();
    private readonly Queue<JobExecutionRecord> _recentJobs = new();
    private readonly Lock _recentLock = new();
    private const int MaxRecentJobs = 64;
    private bool _disposed;

    public JobSystemService(JobServiceConfig config)
        => _config = config;

    public int PendingJobCount
    {
        get
        {
            lock (_queueLock)
            {
                return _jobQueue.Count;
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

    public double MinExecutionTimeMs
    {
        get
        {
            lock (_metricsLock)
            {
                return _minExecutionTimeMs;
            }
        }
    }

    public double MaxExecutionTimeMs
    {
        get
        {
            lock (_metricsLock)
            {
                return _maxExecutionTimeMs;
            }
        }
    }

    public IReadOnlyList<JobExecutionRecord> RecentJobs
    {
        get
        {
            lock (_recentLock)
            {
                return _recentJobs.ToArray();
            }
        }
    }

    public IJobHandle Schedule(IJob job, JobPriority priority = JobPriority.Normal, Action? onComplete = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(job);

        var scheduled = new ScheduledJob
        {
            Name = job.Name,
            Priority = priority,
            OnComplete = onComplete,
            CancellationToken = default
        };
        var handle = new JobHandle(scheduled);

        scheduled.ExecuteAction = async ct =>
                                  {
                                      await Task.Run(job.Execute, ct);
                                      handle.SetResult();
                                  };

        EnqueueJob(scheduled);

        return handle;
    }

    public IJobHandle Schedule(
        IAsyncJob job,
        JobPriority priority = JobPriority.Normal,
        Action? onComplete = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(job);

        var scheduled = new ScheduledJob
        {
            Name = job.Name,
            Priority = priority,
            OnComplete = onComplete,
            CancellationToken = cancellationToken
        };
        var handle = new JobHandle(scheduled);

        scheduled.ExecuteAction = async ct =>
                                  {
                                      await job.ExecuteAsync(ct);
                                      handle.SetResult();
                                  };

        EnqueueJob(scheduled);

        return handle;
    }

    public IJobHandle<TResult> Schedule<TResult>(
        IJob<TResult> job,
        JobPriority priority = JobPriority.Normal,
        Action<TResult>? onComplete = null
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(job);

        var scheduled = new ScheduledJob
        {
            Name = job.Name,
            Priority = priority,
            CancellationToken = default
        };
        var handle = new JobHandle<TResult>(scheduled);

        scheduled.ExecuteAction = async ct =>
                                  {
                                      var result = await Task.Run(job.Execute, ct);
                                      handle.SetResult(result);
                                      onComplete?.Invoke(result);
                                  };

        EnqueueJob(scheduled);

        return handle;
    }

    public IJobHandle<TResult> Schedule<TResult>(
        IAsyncJob<TResult> job,
        JobPriority priority = JobPriority.Normal,
        Action<TResult>? onComplete = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(job);

        var scheduled = new ScheduledJob
        {
            Name = job.Name,
            Priority = priority,
            CancellationToken = cancellationToken
        };
        var handle = new JobHandle<TResult>(scheduled);

        scheduled.ExecuteAction = async ct =>
                                  {
                                      var result = await job.ExecuteAsync(ct);
                                      handle.SetResult(result);
                                      onComplete?.Invoke(result);
                                  };

        EnqueueJob(scheduled);

        return handle;
    }

    public IJobHandle Schedule(
        string name,
        Func<CancellationToken, Task> action,
        JobPriority priority = JobPriority.Normal,
        Action? onComplete = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(action);

        var scheduled = new ScheduledJob
        {
            Name = name,
            Priority = priority,
            OnComplete = onComplete,
            CancellationToken = cancellationToken
        };
        var handle = new JobHandle(scheduled);

        scheduled.ExecuteAction = async ct =>
                                  {
                                      await action(ct);
                                      handle.SetResult();
                                  };

        EnqueueJob(scheduled);

        return handle;
    }

    public IJobHandle<TResult> Schedule<TResult>(
        string name,
        Func<CancellationToken, Task<TResult>> action,
        JobPriority priority = JobPriority.Normal,
        Action<TResult>? onComplete = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(action);

        var scheduled = new ScheduledJob
        {
            Name = name,
            Priority = priority,
            CancellationToken = cancellationToken
        };
        var handle = new JobHandle<TResult>(scheduled);

        scheduled.ExecuteAction = async ct =>
                                  {
                                      var result = await action(ct);
                                      handle.SetResult(result);
                                      onComplete?.Invoke(result);
                                  };

        EnqueueJob(scheduled);

        return handle;
    }

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
                Name = $"LillyEngine-JobWorker-{i}"
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

    public async Task StartAsync()
        => Initialize(_config.WorkerCount);

    public async Task ShutdownAsync()
        => Shutdown();

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
        _queueSignal.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(JobSystemService));
    }

    private void EnqueueJob(ScheduledJob job)
    {
        lock (_queueLock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JobSystemService));

            _jobQueue.Enqueue(job, job.Priority);
            _queueSignal.Release();
        }
    }

    private void Worker(CancellationToken token)
    {
        Interlocked.Increment(ref _activeWorkerCount);

        try
        {
            while (!token.IsCancellationRequested)
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
                    ExecuteJob(job, token);
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref _activeWorkerCount);
        }
    }

    private bool TryDequeueJob(out ScheduledJob job)
    {
        lock (_queueLock)
        {
            return _jobQueue.TryDequeue(out job, out _);
        }
    }

    private void ExecuteJob(ScheduledJob job, CancellationToken serviceToken)
    {
        var stopwatch = Stopwatch.GetTimestamp();
        TimeSpan elapsed;

        try
        {
            if (job.IsCancelled)
            {
                elapsed = Stopwatch.GetElapsedTime(stopwatch);
                Interlocked.Increment(ref _cancelledJobsCount);
                RecordRecentJob(job, elapsed, JobExecutionStatus.Cancelled);
                _logger.Debug("Job {jobName} was cancelled before execution", job.Name);

                return;
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serviceToken, job.CancellationToken);

            job.ExecuteAction!(linkedCts.Token).GetAwaiter().GetResult();
            elapsed = Stopwatch.GetElapsedTime(stopwatch);
            UpdateMetrics(elapsed);
            RecordRecentJob(job, elapsed, JobExecutionStatus.Succeeded);

            job.OnComplete?.Invoke();

            _logger.Debug(
                "Executed job {jobName} (priority: {priority}) in {elapsedMilliseconds} ms",
                job.Name,
                job.Priority,
                elapsed.TotalMilliseconds
            );
        }
        catch (OperationCanceledException)
        {
            elapsed = Stopwatch.GetElapsedTime(stopwatch);
            Interlocked.Increment(ref _cancelledJobsCount);
            RecordRecentJob(job, elapsed, JobExecutionStatus.Cancelled);

            _logger.Debug(
                "Job {jobName} (priority: {priority}) cancelled after {elapsedMilliseconds} ms",
                job.Name,
                job.Priority,
                elapsed.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            elapsed = Stopwatch.GetElapsedTime(stopwatch);
            Interlocked.Increment(ref _failedJobsCount);
            RecordRecentJob(job, elapsed, JobExecutionStatus.Failed);

            _logger.Error(
                ex,
                "Error executing job {jobName} (priority: {priority})",
                job.Name,
                job.Priority
            );
        }
    }

    private void UpdateMetrics(TimeSpan elapsed)
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

    private void RecordRecentJob(ScheduledJob job, TimeSpan elapsed, JobExecutionStatus status)
    {
        var record = new JobExecutionRecord(
            job.Name,
            job.Priority,
            status,
            elapsed.TotalMilliseconds,
            DateTime.UtcNow,
            Environment.CurrentManagedThreadId
        );

        lock (_recentLock)
        {
            if (_recentJobs.Count >= MaxRecentJobs)
                _recentJobs.Dequeue();

            _recentJobs.Enqueue(record);
        }
    }
}
