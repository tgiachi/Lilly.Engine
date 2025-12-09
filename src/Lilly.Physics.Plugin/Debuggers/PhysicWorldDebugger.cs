using ImGuiNET;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;

namespace Lilly.Physics.Plugin.Debuggers;

public class PhysicWorldDebugger : BaseImGuiDebuggerGameObject
{
    private readonly IPhysicWorld3d _physicWorld3d;

    public PhysicWorldDebugger(IPhysicWorld3d physicWorld3d) : base("Physic world Debugger")
        => _physicWorld3d = physicWorld3d;

    protected override void DrawDebug()
    {
        var stats = _physicWorld3d.GetStats();

        if (ImGui.BeginTable("physic_world_stats", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Metric");
            ImGui.TableSetupColumn("Value");
            ImGui.TableHeadersRow();

            DrawRow("Dynamic bodies (total)", stats.DynamicBodies);
            DrawRow("Dynamic bodies (active)", stats.ActiveDynamicBodies);
            DrawRow("Static bodies", stats.StaticBodies);
            DrawRow("Shapes cached", stats.ShapeCount);
            DrawRow("Worker threads", stats.ThreadCount);
            DrawRow("Gravity", $"{stats.Gravity.X:F2}, {stats.Gravity.Y:F2}, {stats.Gravity.Z:F2}");

            ImGui.EndTable();
        }

        // Gravity edit
        var gravityVec = stats.Gravity;

        if (ImGui.InputFloat3("Gravity", ref gravityVec, "%.2f"))
        {
            _physicWorld3d.SetGravity(gravityVec);
        }

        if (ImGui.Button("Wake all bodies"))
        {
            _physicWorld3d.WakeAllBodies();
        }
    }

    private static void DrawRow(string name, int value)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(name);
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(value.ToString());
    }

    private static void DrawRow(string name, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(name);
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(value);
    }
}
