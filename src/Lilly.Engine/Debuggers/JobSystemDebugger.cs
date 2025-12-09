using System.Numerics;
using ImGuiNET;
using Lilly.Engine.Core.Data.Dispatchers;
using Lilly.Engine.Core.Data.Services;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.GameObjects.Base;

namespace Lilly.Engine.Debuggers;

public class JobSystemDebugger : BaseImGuiDebuggerGameObject
{
    private readonly IJobSystemService _jobSystemService;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;

    public JobSystemDebugger(IJobSystemService jobSystemService, IMainThreadDispatcher mainThreadDispatcher) : base(
        "Job Manager Debugger"
    )
    {
        _jobSystemService = jobSystemService;
        _mainThreadDispatcher = mainThreadDispatcher;
    }

    protected override void DrawDebug()
    {
        var pending = _jobSystemService.PendingJobCount;
        var totalExecuted = _jobSystemService.TotalJobsExecuted;
        var cancelled = _jobSystemService.CancelledJobsCount;
        var failed = _jobSystemService.FailedJobsCount;
        var activeWorkers = _jobSystemService.ActiveWorkerCount;
        var totalWorkers = _jobSystemService.TotalWorkerCount;

        var avgMs = _jobSystemService.AverageExecutionTimeMs;
        var minMs = _jobSystemService.MinExecutionTimeMs;
        var maxMs = _jobSystemService.MaxExecutionTimeMs;

        ImGui.SeparatorText("Workers");
        ImGui.Text($"Active Workers: {activeWorkers} / {totalWorkers}");

        var queueLoad = totalWorkers > 0
                            ? Math.Clamp(pending / (float)(totalWorkers * 4), 0f, 1f)
                            : 0f;

        ImGui.Text($"Pending Jobs: {pending}");
        ImGui.ProgressBar(queueLoad, new(-1, 0), $"Queue load ~ {queueLoad * 100f:F0}%");

        ImGui.Spacing();
        ImGui.SeparatorText("Totals");
        ImGui.Text($"Executed: {totalExecuted:N0}");
        ImGui.Text($"Cancelled: {cancelled:N0}");
        ImGui.Text($"Failed: {failed:N0}");

        ImGui.Spacing();
        ImGui.SeparatorText("Execution Time (ms)");
        ImGui.Text($"Average: {avgMs:F2}");
        ImGui.Text($"Min: {(minMs == double.MaxValue ? "n/a" : $"{minMs:F2}")}");
        ImGui.Text($"Max: {(maxMs == 0 ? "n/a" : $"{maxMs:F2}")}");

        ImGui.Spacing();
        ImGui.SeparatorText("Main Thread Dispatcher");
        var queuedActions = _mainThreadDispatcher.QueuedActionCount;
        var concurrentActions = _mainThreadDispatcher.ConcurrentActionCount;
        var totalActions = _mainThreadDispatcher.TotalActionsExecuted;
        var failedActions = _mainThreadDispatcher.FailedActionsCount;
        var avgActionMs = _mainThreadDispatcher.AverageExecutionTimeMs;

        ImGui.Text($"Queued Actions: {queuedActions}");
        ImGui.Text($"Actions per Update: {concurrentActions}");

        var actionLoad = concurrentActions > 0
                             ? Math.Clamp(queuedActions / (float)(concurrentActions * 10), 0f, 1f)
                             : 0f;
        ImGui.ProgressBar(actionLoad, new(-1, 0), $"Action queue ~ {actionLoad * 100f:F0}%");

        ImGui.Text($"Total Executed: {totalActions:N0}");
        ImGui.Text($"Failed: {failedActions:N0}");
        ImGui.Text($"Average Time: {avgActionMs:F2} ms");

        ImGui.Spacing();
        ImGui.SeparatorText("Recent Actions");

        var recentActions = _mainThreadDispatcher.RecentActions;

        if (recentActions.Count == 0)
        {
            ImGui.Text("No actions executed yet.");
        }
        else
        {
            if (ImGui.BeginTable(
                    "RecentActionsTable",
                    4,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY,
                    new(-1, 150)
                ))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Age", ImGuiTableColumnFlags.WidthFixed, 90);
                ImGui.TableHeadersRow();

                var now = DateTime.UtcNow;
                var rowsShown = 0;
                const int maxRows = 20;

                for (var i = recentActions.Count - 1; i >= 0 && rowsShown < maxRows; i--, rowsShown++)
                {
                    var action = recentActions[i];
                    var ageSeconds = (now - action.CompletedAtUtc).TotalSeconds;
                    var ageText = ageSeconds < 60
                                      ? $"{ageSeconds:F1}s ago"
                                      : $"{ageSeconds / 60:F1}m ago";

                    var statusColor = action.Status switch
                    {
                        ActionExecutionStatus.Succeeded => new(0.5f, 0.9f, 0.5f, 1f),
                        ActionExecutionStatus.Failed    => new(0.9f, 0.3f, 0.3f, 1f),
                        _                               => new Vector4(1f, 1f, 1f, 1f)
                    };

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted(action.Name);

                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextColored(statusColor, action.Status.ToString());

                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text($"{action.DurationMs:F2} ms");

                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text(ageText);
                }

                ImGui.EndTable();
            }
        }

        ImGui.Spacing();
        ImGui.SeparatorText("Recent Jobs");

        var recent = _jobSystemService.RecentJobs;

        if (recent.Count == 0)
        {
            ImGui.Text("No jobs executed yet.");

            return;
        }

        if (ImGui.BeginTable(
                "RecentJobsTable",
                5,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY,
                new(-1, 200)
            ))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Priority", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Age", ImGuiTableColumnFlags.WidthFixed, 90);
            ImGui.TableHeadersRow();

            var now = DateTime.UtcNow;
            var rowsShown = 0;
            const int maxRows = 30;

            for (var i = recent.Count - 1; i >= 0 && rowsShown < maxRows; i--, rowsShown++)
            {
                var job = recent[i];
                var ageSeconds = (now - job.CompletedAtUtc).TotalSeconds;
                var ageText = ageSeconds < 60
                                  ? $"{ageSeconds:F1}s ago"
                                  : $"{ageSeconds / 60:F1}m ago";

                var statusColor = job.Status switch
                {
                    JobExecutionStatus.Succeeded => new(0.5f, 0.9f, 0.5f, 1f),
                    JobExecutionStatus.Cancelled => new(0.9f, 0.7f, 0.2f, 1f),
                    JobExecutionStatus.Failed    => new(0.9f, 0.3f, 0.3f, 1f),
                    _                            => new Vector4(1f, 1f, 1f, 1f)
                };

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(job.Name);

                ImGui.TableSetColumnIndex(1);
                ImGui.Text(job.Priority.ToString());

                ImGui.TableSetColumnIndex(2);
                ImGui.TextColored(statusColor, job.Status.ToString());

                ImGui.TableSetColumnIndex(3);
                ImGui.Text($"{job.DurationMs:F1} ms");

                ImGui.TableSetColumnIndex(4);
                ImGui.Text(ageText);
            }

            ImGui.EndTable();
        }
    }
}
