using System.Runtime.InteropServices;
using ImGuiNET;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;

namespace Lilly.Engine.Debuggers;

public class PerformanceDebugger : BaseImGuiDebuggerGameObject
{
    private readonly IPerformanceProfilerService _profiler;

    public PerformanceDebugger(IPerformanceProfilerService profiler) : base("Performance debugger")
        => _profiler = profiler;

    protected override void DrawDebug()
    {
        DrawSummary();

        ImGui.Separator();
        ImGui.TextUnformatted("Graphs");

        PlotHistory("Frame time (ms)", _profiler.FrameTimeHistory);
        PlotHistory("Update time (ms)", _profiler.UpdateTimeHistory);
        PlotHistory("Draw time (ms)", _profiler.DrawTimeHistory);
        PlotHistory("FPS", _profiler.FpsHistory);

        if (ImGui.Button("Reset metrics"))
        {
            _profiler.ResetMetrics();
        }
    }

    private static void DrawMetricRow(string name, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(name);
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(value);
    }

    private void DrawSummary()
    {
        if (ImGui.BeginTable("perf_summary", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Metric");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            DrawMetricRow("FPS (current / avg)", $"{_profiler.CurrentFps:F1} / {_profiler.AverageFps:F1}");
            DrawMetricRow(
                "Frame time ms (cur / avg / min / max)",
                $"{_profiler.CurrentFrameTime:F3} / {_profiler.AverageFrameTime:F3} / {_profiler.MinFrameTime:F3} / {_profiler.MaxFrameTime:F3}"
            );
            DrawMetricRow("Update time ms (cur)", $"{_profiler.CurrentUpdateTime:F3}");
            DrawMetricRow("Draw time ms (cur / avg)", $"{_profiler.CurrentDrawTime:F3} / {_profiler.AverageDrawTime:F3}");
            DrawMetricRow("Memory MB", $"{_profiler.MemoryUsageMb:F2}");
            DrawMetricRow("Frames", _profiler.TotalFrames.ToString());

            ImGui.EndTable();
        }

        ImGui.Separator();
        ImGui.TextUnformatted("OpenGL Metrics (this frame)");

        if (ImGui.BeginTable("opengl_metrics", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Metric");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            DrawMetricRow("Draw calls", _profiler.DrawCallsThisFrame.ToString());
            DrawMetricRow("Vertices", _profiler.VerticesThisFrame.ToString());
            DrawMetricRow("Triangles", _profiler.TrianglesThisFrame.ToString());
            DrawMetricRow("Texture bindings", _profiler.TextureBindingsThisFrame.ToString());
            DrawMetricRow("Shader switches", _profiler.ShaderSwitchesThisFrame.ToString());

            ImGui.EndTable();
        }
    }

    private static (float min, float max) GetMinMax(Span<float> span)
    {
        if (span.IsEmpty)
        {
            return (0f, 0f);
        }

        var min = span[0];
        var max = span[0];

        for (var i = 1; i < span.Length; i++)
        {
            var value = span[i];

            if (value < min)
            {
                min = value;
            }

            if (value > max)
            {
                max = value;
            }
        }

        return (min, max);
    }

    private static void PlotHistory(string label, IReadOnlyList<double> data)
    {
        if (data.Count == 0)
        {
            ImGui.TextUnformatted($"{label}: no data");

            return;
        }

        var length = data.Count;
        var buffer = length <= 256
                         ? stackalloc float[256]
                         : new float[length];

        for (var i = 0; i < length; i++)
        {
            buffer[i] = (float)data[i];
        }

        var span = buffer[..length];
        var (min, max) = GetMinMax(span);

        ImGui.PlotLines(
            label,
            ref MemoryMarshal.GetReference(span),
            span.Length,
            0,
            null,
            min,
            max,
            new(-1, 60),
            sizeof(float)
        );
    }
}
