using Lilly.Voxel.Plugin.Actionables;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Interfaces.Actionables;

/// <summary>
/// Listener interface for handling actionable events in the voxel world.
/// </summary>
public interface IActionableListener
{
    ActionEventType EventType { get; }

    /// <summary>
    /// Determines if this listener can handle the given block instance.
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    bool CanHandle(BlockInstance instance);

    /// <summary>
    /// Dispatches an action event to the listener.
    /// </summary>
    /// <param name="actionEventContext"></param>
    void DispatchAction(ActionEventContext actionEventContext);
}
