using ImGuiNET;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Debuggers;
using Lilly.Engine.Layers;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Helpers;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Debuggers;

/// <summary>
/// Provides an ImGui debugger for displaying render pipeline diagnostics.
/// </summary>
public class RenderPipelineDiagnosticsDebugger : IImGuiDebugger
{
    private readonly IGraphicRenderPipeline _renderPipeline;
    private readonly RenderLayerSystem3D _renderLayerSystem3D;
    private bool _showDetailedStats = true;
    private bool _showLayerBreakdown = true;

    /// <summary>
    /// Gets or sets the unique identifier for this debugger.
    /// </summary>
    public uint Id { get; set; }




    /// <summary>
    /// Gets or sets the name of this debugger.
    /// </summary>
    public string Name { get; set; } = "Render Pipeline Diagnostics";

    /// <summary>
    /// Gets the rendering order.
    /// </summary>
    public ushort Order { get; } = 1000;

    /// <summary>
    /// Gets or sets whether this debugger is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the render layer for this debugger.
    /// </summary>
    public RenderLayer Layer { get; set; } = RenderLayer.Debug;

    /// <summary>
    /// Gets or sets the parent game object.
    /// </summary>
    public IGameObject? Parent { get; set; }

    /// <summary>
    /// Gets the collection of child game objects.
    /// </summary>
    public GameObjectCollection<IGameObject> Children { get; } = new();

    /// <summary>
    /// Initializes a new instance of the RenderPipelineDiagnosticsDebugger class.
    /// </summary>
    /// <param name="renderPipeline">The render pipeline to monitor.</param>
    public RenderPipelineDiagnosticsDebugger(IGraphicRenderPipeline renderPipeline)
    {
        _renderPipeline = renderPipeline;
        _renderLayerSystem3D = _renderPipeline.GetRenderLayerSystem<RenderLayerSystem3D>();
    }

    /// <summary>
    /// Renders the game object (IGameObject implementation).
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public IEnumerable<RenderCommand> Render(GameTime gameTime)
    {
        yield return RenderCommandHelpers.ImGuiRender(new(Name, Render));
    }

    /// <summary>
    /// Renders the diagnostics window using ImGui.
    /// </summary>
    /// <summary>
    /// Renders the diagnostics window using ImGui.
    /// </summary>
    public void Render()
    {
        if (!IsVisible)
        {
            return;
        }

        var diagnostics = _renderPipeline.Diagnostics;

        // === Overall Statistics ===
        ImGui.SeparatorText("Overall Statistics");

        ImGui.Text($"Total Frames:         {diagnostics.TotalFrames:N0}");
        ImGui.Text($"Current FPS:          {diagnostics.CurrentFPS:F2}");

        ImGui.Spacing();

        // Commands statistics with color coding
        ImGui.Text("Commands This Frame:  ");
        ImGui.SameLine();
        var currentCommands = diagnostics.TotalCommandsThisFrame;
        var avgCommands = diagnostics.AverageCommandsPerFrame;

        // Color code based on performance
        if (currentCommands > avgCommands * 1.5)
        {
            ImGui.TextColored(new(1, 0, 0, 1), $"{currentCommands:N0}"); // Red
        }
        else if (currentCommands > avgCommands * 1.2)
        {
            ImGui.TextColored(new(1, 1, 0, 1), $"{currentCommands:N0}"); // Yellow
        }
        else
        {
            ImGui.TextColored(new(0, 1, 0, 1), $"{currentCommands:N0}"); // Green
        }

        ImGui.Text($"Average Cmd/Frame:    {diagnostics.AverageCommandsPerFrame:F2}");
        ImGui.Text($"Peak Cmd/Frame:       {diagnostics.PeakCommandsPerFrame:N0}");

        ImGui.Spacing();

        // === Options ===
        ImGui.Checkbox("Show Detailed Stats", ref _showDetailedStats);
        ImGui.SameLine();
        ImGui.Checkbox("Show Layer Breakdown", ref _showLayerBreakdown);

        if (ImGui.Button("Reset Statistics"))
        {
            diagnostics.Reset();
        }

        // === Layer Breakdown ===
        if (_showLayerBreakdown && diagnostics.LayerStatistics.Count > 0)
        {
            ImGui.Spacing();
            ImGui.SeparatorText("Layer Breakdown");

            if (ImGui.BeginTable(
                    "LayerStats",
                    5,
                    ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable
                ))
            {
                // Header
                ImGui.TableSetupColumn("Layer", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Current", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Average", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Peak", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Total", ImGuiTableColumnFlags.WidthFixed, 90);
                ImGui.TableHeadersRow();

                // Data rows
                foreach (var (layerName, stats) in
                         diagnostics.LayerStatistics.OrderByDescending(kvp => kvp.Value.LayerOrder))
                {
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(layerName);

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text($"{stats.CommandsThisFrame:N0}");

                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text($"{stats.AverageCommandsPerFrame:F2}");

                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text($"{stats.PeakCommands:N0}");

                    ImGui.TableSetColumnIndex(4);
                    ImGui.Text($"{stats.TotalCommandsProcessed:N0}");
                }

                ImGui.EndTable();
            }
        }

        // === Detailed Statistics (Optional) ===
        if (_showDetailedStats)
        {
            ImGui.Spacing();
            ImGui.SeparatorText("Performance Metrics");

            var layerCount = diagnostics.LayerStatistics.Count;
            var avgCommandsPerLayer = layerCount > 0 ? diagnostics.TotalCommandsThisFrame / (double)layerCount : 0;

            ImGui.Text($"Active Layers:        {layerCount}");
            ImGui.Text($"Avg Cmd/Layer:        {avgCommandsPerLayer:F2}");

            // Performance bar
            ImGui.Spacing();
            ImGui.Text("Command Load:");
            var loadPercentage = diagnostics.PeakCommandsPerFrame > 0
                                     ? (float)diagnostics.TotalCommandsThisFrame / diagnostics.PeakCommandsPerFrame
                                     : 0f;

            ImGui.ProgressBar(loadPercentage, new(0, 0), $"{loadPercentage * 100:F1}%");
        }

        ImGui.Text($"Objects rendered this frame: {_renderLayerSystem3D.ObjectInFrustum.Count:N0}");
        ImGui.Spacing();
        ImGui.Text($"Object not rendered  this frame: {_renderLayerSystem3D.ObjectOutOfFrustum.Count:N0}");

        // === Export ===
        ImGui.Spacing();

        if (ImGui.Button("Copy Summary to Clipboard"))
        {
            ImGui.SetClipboardText(diagnostics.GetSummary());
        }
    }
}
