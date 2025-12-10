using Lilly.Voxel.Plugin.Interfaces.Actionables;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Actionables.Listeners.Base;

/// <summary>
/// Base listener that gates dispatch using a component query (All/Any/None).
/// </summary>
public abstract class ComponentQueryListener : IActionableListener
{
    /// <summary>
    /// Event type this listener responds to.
    /// </summary>
    public abstract ActionEventType EventType { get; }

    /// <summary>
    /// Component query used to decide if this listener can handle the instance.
    /// </summary>
    protected abstract ComponentQuery Query { get; }

    /// <summary>
    /// Determines if the instance satisfies this listener's component requirements.
    /// </summary>
    public virtual bool CanHandle(IActionableTarget target)
        => Query.Matches(target.Components);

    /// <summary>
    /// Dispatches an action event to the listener.
    /// </summary>
    /// <param name="actionEventContext"></param>
    public abstract void DispatchAction(ActionEventContext actionEventContext);
}
