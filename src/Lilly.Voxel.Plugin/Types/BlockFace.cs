namespace Lilly.Voxel.Plugin.Types;

/// <summary>
/// Defines the faces of a block in 3D space.
/// </summary>
public enum BlockFace : byte
{
    Top, // +Y
    Bottom, // -Y
    Front, // +Z
    Back, // -Z
    Left, // -X
    Right // +X
}
