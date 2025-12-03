using Lilly.Voxel.Plugin.Collections;

namespace Lilly.Voxel.Plugin.Blocks;

public record BlockInstance(int LocalIndex, ushort BlockTypeId)
{
    public ComponentBag Components { get; init; } = new();
    public Guid InstanceId { get; init; } = Guid.NewGuid();

}
