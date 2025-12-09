using Lilly.Engine.Core.Extensions.Strings;
using Lilly.Engine.Interfaces.Debuggers;
using Lilly.Rendering.Core.Interfaces.Entities;

namespace Lilly.Engine.GameObjects.Base;

public abstract class BaseImGuiDebuggerGameObject : IImGuiDebugger
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public uint ZIndex { get; set; }
    public bool IsActive { get; set; }
    public IGameObject? Parent { get; set; }
    public IEnumerable<IGameObject> Children { get; } = [];
    public string Title { get; set; }

    protected BaseImGuiDebuggerGameObject(string title)
    {
        Title = title;
        Name = title.ToSnakeCase();
    }

    public void Draw()
    {
        DrawDebug();
    }

    public void OnRemoved() { }

    protected virtual void DrawDebug() { }
}
