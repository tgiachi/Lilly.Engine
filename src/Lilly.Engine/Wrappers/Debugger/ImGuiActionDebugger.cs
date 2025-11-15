using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Debuggers;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Helpers;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Wrappers.Debugger;

public class ImGuiActionDebugger : IImGuiDebugger
{
    public IGameObject? Parent { get; set; }
    public GameObjectCollection<IGameObject> Children { get; } = new();
    public uint Id { get; set; }
    public string Name { get; set; }
    public ushort Order { get; }

    private readonly Action _callback;

    /// <summary>
    /// Initializes a new instance of the ImGuiActionDebugger class.
    /// </summary>
    /// <param name="name">The name of the debugger.</param>
    /// <param name="callback">The action to execute for rendering.</param>
    public ImGuiActionDebugger(string name, Action callback)
    {
        Name = name;
        _callback = callback;
    }

    /// <summary>
    /// Renders the debugger by yielding an ImGui render command.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <returns>An enumerable of render commands.</returns>
    public IEnumerable<RenderCommand> Render(GameTime gameTime)
    {
        yield return RenderCommandHelpers.ImGuiRender(new(Name, _callback));
    }
}
