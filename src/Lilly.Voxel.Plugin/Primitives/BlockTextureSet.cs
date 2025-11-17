using Lilly.Voxel.Plugin.Types;
using Squid.Engine.World.Voxels.Types;

namespace Squid.Engine.World.Voxels.Primitives;

/// <summary>
/// Defines a set of textures for different faces of a block.
/// </summary>
public class BlockTextureSet
{
    public BlockTextureObject Top { get; set; }
    public BlockTextureObject Bottom { get; set; }
    public BlockTextureObject Left { get; set; }
    public BlockTextureObject Right { get; set; }
    public BlockTextureObject Front { get; set; }
    public BlockTextureObject Back { get; set; }

    public BlockTextureSet() { }

    public BlockTextureSet(BlockTextureObject allFaces)
    {
        Top = Bottom = Left = Right = Front = Back = Front = Back = allFaces;
    }

    public BlockTextureSet(
        BlockTextureObject top,
        BlockTextureObject bottom,
        BlockTextureObject left,
        BlockTextureObject right,
        BlockTextureObject front,
        BlockTextureObject back
    )
    {
        Top = top;
        Bottom = bottom;
        Left = left;
        Right = right;
        Front = front;
        Back = back;
    }

    public BlockTextureSet(BlockTextureObject top, BlockTextureObject bottom, BlockTextureObject sides)
    {
        Top = top;
        Bottom = bottom;
        Left = Right = Front = Back = sides;
    }

    public BlockTextureObject GetTextureForFace(BlockFace face)
    {
        return face switch
        {
            BlockFace.Top    => Top,
            BlockFace.Bottom => Bottom,
            BlockFace.Left   => Left,
            BlockFace.Right  => Right,
            BlockFace.Front  => Front,
            BlockFace.Back   => Back,
            _                => throw new ArgumentOutOfRangeException(nameof(face), face, null)
        };
    }

    public void SetTexture(BlockFace face, string assetName, int index)
    {
        var textureObject = new BlockTextureObject(assetName, index);

        switch (face)
        {
            case BlockFace.Top:
                Top = textureObject;

                break;
            case BlockFace.Bottom:
                Bottom = textureObject;

                break;
            case BlockFace.Left:
                Left = textureObject;

                break;
            case BlockFace.Right:
                Right = textureObject;

                break;
            case BlockFace.Front:
                Front = textureObject;

                break;
            case BlockFace.Back:
                Back = textureObject;

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(face), face, null);
        }
    }
}
