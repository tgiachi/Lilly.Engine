using Lilly.Engine.Core.Data.Services;

namespace Lilly.Engine.Core.Interfaces.Services;

/// <summary>
/// Provides diagnostic metrics about the job system.
/// </summary>
public interface IJobSystemMetrics
{
    /// <summary>
    /// Gets the number of jobs currently pending execution.
    /// </summary>
    int PendingJobCount { get; }

    /// <summary>
    /// Gets the total number of jobs executed since initialization.
    /// </summary>
    long TotalJobsExecuted { get; }

    /// <summary>
    /// Gets the average execution time of completed jobs in milliseconds.
    /// </summary>
    double AverageExecutionTimeMs { get; }

    /// <summary>
    /// Gets the minimum execution time of any completed job in milliseconds.
    /// </summary>
    double MinExecutionTimeMs { get; }

    /// <summary>
    /// Gets the maximum execution time of any completed job in milliseconds.
    /// </summary>
    double MaxExecutionTimeMs { get; }

    /// <summary>
    /// Gets the number of currently active worker threads.
    /// </summary>
    int ActiveWorkerCount { get; }

    /// <summary>
    /// Gets the total number of initialized worker threads.
    /// </summary>
    int TotalWorkerCount { get; }

    /// <summary>
    /// Gets the number of jobs that were cancelled.
    /// </summary>
    long CancelledJobsCount { get; }

    /// <summary>
    /// Gets the number of jobs that failed with exceptions.
    /// </summary>
    long FailedJobsCount { get; }

    /// <summary>
    /// Gets a snapshot of the most recently finished jobs.
    /// </summary>
    IReadOnlyList<JobExecutionRecord> RecentJobs { get; }
}
