using Lilly.Voxel.Plugin.Collections;
using Lilly.Voxel.Plugin.Interfaces.Actionables;

namespace Lilly.Voxel.Plugin.Blocks;

public record BlockInstance(int LocalIndex, ushort BlockTypeId) : IActionableTarget
{
    public ComponentBag Components { get; init; } = new();
    public Guid TargetId { get; init; } = Guid.NewGuid();
}
