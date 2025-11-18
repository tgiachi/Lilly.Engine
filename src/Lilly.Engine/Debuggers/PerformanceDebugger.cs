using System.Numerics;
using ImGuiNET;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Debuggers;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Debuggers;

/// <summary>
/// ImGui debugger for displaying real-time performance metrics
/// </summary>
public class PerformanceDebugger : IImGuiDebugger
{
    public IGameObject? Parent { get; set; }
    public GameObjectCollection<IGameObject> Children { get; } = new();
    public uint Id { get; set; }
    public string Name { get; set; } = "Performance Debugger";
    public ushort Order { get; } = 1000;

    private readonly IPerformanceProfilerService _profilerService;

    public bool IsVisible { get; set; } = true;
    public string WindowTitle { get; set; } = "Performance Debugger";

    public PerformanceDebugger(IPerformanceProfilerService profilerService)
        => _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));

    public IEnumerable<RenderCommand> Render(GameTime gameTime)
    {
        DrawMainMetrics();
        ImGui.Separator();

        if (ImGui.CollapsingHeader("Detailed Metrics", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawDetailedMetrics();
        }

        if (ImGui.CollapsingHeader("Performance Graphs", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawPerformanceGraphs();
        }

        if (ImGui.CollapsingHeader("Memory Info", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawMemoryInfo();
        }

        ImGui.Separator();

        if (ImGui.Button("Reset Metrics", new(120, 0)))
        {
            _profilerService.ResetMetrics();
        }

        yield break;
    }

    private void DrawDetailedMetrics()
    {
        ImGui.Indent();

        // Frame timing details
        ImGui.Text($"Min Frame Time: {_profilerService.MinFrameTime:F2} ms");
        ImGui.Text($"Max Frame Time: {_profilerService.MaxFrameTime:F2} ms");

        // Draw time info
        ImGui.Spacing();
        ImGui.Text($"Update Time: {_profilerService.CurrentUpdateTime:F2} ms");
        ImGui.Text($"Draw Time: {_profilerService.CurrentDrawTime:F2} ms");
        ImGui.SameLine();
        ImGui.Text($"(Avg: {_profilerService.AverageDrawTime:F2} ms)");

        // Total frames
        ImGui.Spacing();
        ImGui.Text($"Total Frames: {_profilerService.TotalFrames}");

        // Frame time distribution bar
        var minFrameTime = _profilerService.MinFrameTime;
        var maxFrameTime = _profilerService.MaxFrameTime;
        var avgFrameTime = _profilerService.AverageFrameTime;

        if (maxFrameTime > minFrameTime)
        {
            var normalizedPosition = (avgFrameTime - minFrameTime) / (maxFrameTime - minFrameTime);
            normalizedPosition = Math.Clamp(normalizedPosition, 0, 1);

            ImGui.ProgressBar(
                (float)normalizedPosition,
                new(-1, 0),
                $"Avg: {avgFrameTime:F2} ms"
            );
        }

        ImGui.Unindent();
    }

    private void DrawMainMetrics()
    {
        var fps = _profilerService.CurrentFps;
        var fpsColor = GetFpsColor(fps);

        ImGui.TextColored(fpsColor, $"FPS: {fps:F1}");
        ImGui.SameLine();
        ImGui.Text($"(Avg: {_profilerService.AverageFps:F1})");

        var frameTime = _profilerService.CurrentFrameTime;
        ImGui.Text($"Frame Time: {frameTime:F2} ms");
        ImGui.SameLine();
        ImGui.Text($"(Avg: {_profilerService.AverageFrameTime:F2} ms)");
    }

    private void DrawMemoryInfo()
    {
        ImGui.Indent();

        var memoryMb = _profilerService.MemoryUsageMb;
        ImGui.Text($"Memory Usage: {memoryMb:F2} MB");

        // Memory progress bar (normalized to 1GB as reference)
        const float referenceMemoryMb = 1024.0f; // 1 GB as reference
        var normalizedMemory = Math.Min((float)(memoryMb / referenceMemoryMb), 1.0f);

        var memoryColor = GetMemoryColor(normalizedMemory);
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, memoryColor);
        ImGui.ProgressBar(
            normalizedMemory,
            new(-1, 0),
            $"{normalizedMemory * 100:F0}% of 1 GB reference"
        );
        ImGui.PopStyleColor();

        ImGui.Spacing();

        if (ImGui.Button("Force GC", new(100, 0)))
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        ImGui.Unindent();
    }

    private void DrawPerformanceGraphs()
    {
        ImGui.Indent();

        var fpsHistory = _profilerService.FpsHistory;

        if (fpsHistory.Count > 0)
        {
            var fpsArray = fpsHistory.Select(x => (float)x).ToArray();
            var fpsMin = fpsArray.Length > 0 ? fpsArray.Min() * 0.9f : 0;
            var fpsMax = fpsArray.Length > 0 ? fpsArray.Max() * 1.1f : 60;

            ImGui.PlotLines(
                "FPS History",
                ref fpsArray[0],
                fpsArray.Length,
                0,
                $"Current: {_profilerService.CurrentFps:F1}",
                fpsMin,
                fpsMax,
                new(ImGui.GetContentRegionAvail().X, 80)
            );
        }

        ImGui.Spacing();

        var frameTimeHistory = _profilerService.FrameTimeHistory;

        if (frameTimeHistory.Count > 0)
        {
            var frameTimeArray = frameTimeHistory.Select(x => (float)x).ToArray();
            var ftMin = frameTimeArray.Length > 0 ? frameTimeArray.Min() * 0.9f : 0;
            var ftMax = frameTimeArray.Length > 0 ? frameTimeArray.Max() * 1.1f : 16.67f;

            ImGui.PlotLines(
                "Frame Time History (ms)",
                ref frameTimeArray[0],
                frameTimeArray.Length,
                0,
                $"Current: {_profilerService.CurrentFrameTime:F2}",
                ftMin,
                ftMax,
                new(ImGui.GetContentRegionAvail().X, 80)
            );
        }

        ImGui.Spacing();

        var drawTimeHistory = _profilerService.DrawTimeHistory;

        if (drawTimeHistory.Count > 0)
        {
            var drawTimeArray = drawTimeHistory.Select(x => (float)x).ToArray();
            var dtMin = drawTimeArray.Length > 0 ? drawTimeArray.Min() * 0.9f : 0;
            var dtMax = drawTimeArray.Length > 0 ? drawTimeArray.Max() * 1.1f : 10;

            ImGui.PlotLines(
                "Draw Time History (ms)",
                ref drawTimeArray[0],
                drawTimeArray.Length,
                0,
                $"Current: {_profilerService.CurrentDrawTime:F2}",
                dtMin,
                dtMax,
                new(ImGui.GetContentRegionAvail().X, 80)
            );
        }

        ImGui.Unindent();
    }

    private static Vector4 GetFpsColor(double fps)
    {
        if (fps >= 50)
        {
            return new(0, 1, 0, 1); // Green - good
        }

        if (fps >= 30)
        {
            return new(1, 1, 0, 1); // Yellow - acceptable
        }

        return new(1, 0, 0, 1); // Red - poor
    }

    private static Vector4 GetMemoryColor(float normalizedMemory)
    {
        if (normalizedMemory < 0.5f)
        {
            return new(0, 1, 0, 1); // Green
        }

        if (normalizedMemory < 0.8f)
        {
            return new(1, 1, 0, 1); // Yellow
        }

        return new(1, 0, 0, 1); // Red
    }
}
