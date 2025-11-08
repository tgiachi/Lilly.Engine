using Lilly.Engine.Commands;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Payloads;
using Lilly.Engine.Interfaces.Debuggers;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Wrappers;

public class ImGuiActionDebugger : IImGuiDebugger
{
    public IGameObject? Parent { get; set; }
    public GameObjectCollection<IGameObject> Children { get; } = new();
    public uint Id { get; set; }
    public string Name { get; }
    public ushort Order { get; }

    private readonly Action _callback;

    public ImGuiActionDebugger(string name, Action callback)
    {
        Name = name;
        _callback = callback;
    }

    public void Render(GameTime gameTime, ref List<RenderCommand> renderCommands)
    {
        renderCommands.Add(RenderCommandHelpers.ImGuiRender(new ImGuiDataPayload(Name, _callback)));
    }


}
