using Lilly.Engine.Commands;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Debuggers;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Payloads;

namespace Lilly.Engine.Debuggers.Base;

public abstract class BaseDebugger : IImGuiDebugger
{
    public IGameObject? Parent { get; set; }
    public GameObjectCollection<IGameObject> Children { get; } = new();
    public uint Id { get; set; }
    public string Name { get; set; }
    public ushort Order { get; }

    protected BaseDebugger(string name)
    {
        Name = name;
    }

    public IEnumerable<RenderCommand> Render(GameTime gameTime)
    {
        yield return RenderCommandHelpers.ImGuiRender(new ImGuiDataPayload(Name, DrawImGui));
    }

    public abstract void DrawImGui();
}
