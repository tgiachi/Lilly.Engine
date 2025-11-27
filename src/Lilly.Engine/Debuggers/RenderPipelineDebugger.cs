using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.Debuggers;

public class RenderPipelineDebugger : BaseImGuiDebuggerGameObject
{
    private const int HistoryLength = 240;

    private readonly IRenderPipeline _renderPipeline;
    private readonly Dictionary<string, List<float>> _renderHistory = new();
    private readonly Dictionary<string, List<float>> _updateHistory = new();

    public RenderPipelineDebugger(IRenderPipeline renderPipeline) : base("Render pipeline")
    {
        _renderPipeline = renderPipeline;
    }

    protected override void DrawDebug()
    {
        var layers = _renderPipeline.RenderLayers
                                    .OrderBy(l => l.Priority)
                                    .ThenBy(l => l.Name)
                                    .ToList();

        if (!layers.Any())
        {
            ImGui.TextUnformatted("No layers registered.");

            return;
        }

        if (ImGui.BeginTable("render_layers", 8, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Priority");
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Active");
            ImGui.TableSetupColumn("Processed");
            ImGui.TableSetupColumn("Skipped");
            ImGui.TableSetupColumn("Total");
            ImGui.TableSetupColumn("Render (ms)");
            ImGui.TableSetupColumn("Update (ms)");
            ImGui.TableHeadersRow();

            foreach (var layer in layers)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Text(layer.Priority.ToString());

                ImGui.TableSetColumnIndex(1);
                ImGui.Text(layer.Name);

                ImGui.TableSetColumnIndex(2);
                var buttonLabel = layer.IsActive ? "Disable" : "Enable";

                if (ImGui.Button($"{buttonLabel}##{layer.Name}"))
                {
                    layer.IsActive = !layer.IsActive;
                }

                ImGui.TableSetColumnIndex(3);
                ImGui.Text(layer.ProcessedEntityCount.ToString());

                ImGui.TableSetColumnIndex(4);
                ImGui.Text(layer.SkippedEntityCount.ToString());

                ImGui.TableSetColumnIndex(5);
                ImGui.Text(layer.TotalEntityCount.ToString());

                ImGui.TableSetColumnIndex(6);
                ImGui.Text($"{layer.RenderTimeMilliseconds:F3}");

                ImGui.TableSetColumnIndex(7);
                ImGui.Text($"{layer.UpdateTimeMilliseconds:F3}");
            }

            ImGui.EndTable();
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Timings (ms) history");


    }
}
