namespace Lilly.Voxel.Plugin.Types;

/// <summary>
/// Defines the rendering types for blocks in the voxel engine.
/// </summary>
public enum BlockRenderType : byte
{
    Solid,
    Transparent,
    Billboard,
    Cutout,
    Item,
    Fluid,
    Clouds
}
