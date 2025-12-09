using Lilly.Engine.Scenes.Transitions.Interfaces;
using Lilly.Rendering.Core.Interfaces.Entities;

namespace Lilly.Engine.Scenes.Transitions.Base;

/// <summary>
/// Base class that makes Transition compatible with the IGameObject interface for rendering pipeline integration.
/// </summary>
public abstract class TransitionGameObject : Transition, IGameObject
{
    protected TransitionGameObject(float duration, ITransitionEffect effect)
        : base(duration, effect) { }

    public IGameObject? Parent { get; set; }

    public IEnumerable<IGameObject> Children => Enumerable.Empty<IGameObject>();

    public uint Id { get; set; }

    public string Name { get; set; } = "Transition";

    public uint ZIndex { get; set; } = 1000; // High z-index to render on top

    public bool IsActive { get; set; } = true;

    public void OnRemoved() { }
}
