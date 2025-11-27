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

        foreach (var layer in layers)
        {
            AddSample(_renderHistory, layer.Name, (float)layer.RenderTimeMilliseconds);
            AddSample(_updateHistory, layer.Name, (float)layer.UpdateTimeMilliseconds);
        }

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

        foreach (var layer in layers)
        {
            ImGui.PushID(layer.Name);
            ImGui.TextUnformatted(layer.Name);
            PlotHistory("Render", _renderHistory, layer.Name);
            PlotHistory("Update", _updateHistory, layer.Name);
            ImGui.PopID();
        }
    }

    private static void AddSample(Dictionary<string, List<float>> history, string key, float value)
    {
        if (!history.TryGetValue(key, out var list))
        {
            list = new List<float>();
            history[key] = list;
        }

        list.Add(value);

        if (list.Count > HistoryLength)
        {
            list.RemoveAt(0);
        }
    }

    private static void PlotHistory(string label, Dictionary<string, List<float>> history, string key)
    {
        if (!history.TryGetValue(key, out var samples) || samples.Count == 0)
        {
            ImGui.TextUnformatted($"{label}: no data");
            return;
        }

        var span = CollectionsMarshal.AsSpan(samples);
        var graphSize = new Vector2(-1, 60);

        ImGui.PlotLines(
            label,
            ref MemoryMarshal.GetReference(span),
            span.Length,
            0,
            null,
            0f,
            float.MaxValue,
            graphSize,
            sizeof(float)
        );
    }
}
