using System;
using Lilly.Voxel.Plugin.Collections;

namespace Lilly.Voxel.Plugin.Interfaces.Actionables;

/// <summary>
/// Generic actionable target (block, model, etc.) that can carry components.
/// </summary>
public interface IActionableTarget
{
    ComponentBag Components { get; }
    Guid TargetId { get; }
}
