using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Scenes.Transitions.Base;

/// <summary>
/// Base class that makes Transition compatible with the IGameObject interface for rendering pipeline integration.
/// </summary>
public abstract class TransitionGameObject : Transition, IGameObject
{
    private IGameObject? _parent;
    private readonly GameObjectCollection<IGameObject> _children = new();

    protected TransitionGameObject(float duration, Interfaces.ITransitionEffect effect)
        : base(duration, effect)
    {
    }

    public IGameObject? Parent
    {
        get => _parent;
        set => _parent = value;
    }

    public GameObjectCollection<IGameObject> Children => _children;

    public uint Id { get; set; }

    public string Name { get; set; } = "Transition";

    public ushort Order { get; set; }

    public IEnumerable<RenderCommand> Render(GameTime gameTime)
    {
        // For now, transitions render directly via the Effect and RenderPipeline
        // This method is required by IGameObject but transitions use a different rendering path
        return Enumerable.Empty<RenderCommand>();
    }
}
