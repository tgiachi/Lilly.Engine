using Lilly.Rendering.Core.Interfaces.Entities;

namespace Lilly.Engine.Interfaces.Debuggers;

/// <summary>
/// Represents a game object that provides ImGui debugging functionality.
/// </summary>
public interface IImGuiDebugger : IGameObject
{
    string Title { get; set; }

    void Draw();
}
