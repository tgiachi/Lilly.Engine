using Lilly.Voxel.Plugin.Types;
using Squid.Engine.World.Voxels.Primitives;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Primitives;

/// <summary>
/// Represents a type of block in the voxel world.
/// </summary>
public class BlockType
{
    /// <summary>
    /// Gets or sets the unique identifier for this block type.
    /// </summary>
    public ushort Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this block type.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets whether this block is solid.
    /// </summary>
    public bool IsSolid { get; set; }

    /// <summary>
    /// Gets or sets whether this block is a liquid.
    /// </summary>
    public bool IsLiquid { get; set; }

    /// <summary>
    /// Gets or sets whether this block is opaque.
    /// </summary>
    public bool IsOpaque { get; set; }

    /// <summary>
    /// Gets or sets whether this block is transparent.
    /// </summary>
    public bool IsTransparent { get; set; }

    /// <summary>
    /// Gets or sets the hardness of this block.
    /// </summary>
    public float Hardness { get; set; }

    /// <summary>
    /// Gets or sets whether this block can be broken.
    /// </summary>
    public bool IsBreakable { get; set; }

    public bool IsBillboard { get; set; }

    public bool IsItem { get; set; }

    public bool IsLightSource => EmitsLight > 0 && EmitsColor != Color4b.Transparent;

    public float EmitsLight { get; set; }

    public Color4b EmitsColor { get; set; } = Color4b.Transparent;

    /// <summary>
    /// Gets or sets the texture set for this block.
    /// </summary>
    public BlockTextureSet TextureSet { get; set; } = new();

    /// <summary>
    /// Gets the render type for this block based on its properties.
    /// </summary>
    public BlockRenderType RenderType
    {
        get
        {
            if (IsTransparent)
            {
                return BlockRenderType.Transparent;
            }

            if (IsSolid)
            {
                return BlockRenderType.Solid;
            }

            if (IsOpaque)
            {
                return BlockRenderType.Cutout;
            }

            if (IsLiquid)
            {
                return BlockRenderType.Fluid;
            }

            if (IsItem)
            {
                return BlockRenderType.Item;
            }

            if (IsBillboard)
            {
                return BlockRenderType.Billboard;
            }

            return BlockRenderType.Transparent;
        }
    }

    /// <summary>
    /// Initializes a new instance of the BlockType class.
    /// </summary>
    public BlockType() { }

    /// <summary>
    /// Initializes a new instance of the BlockType class with the specified id and name.
    /// </summary>
    /// <param name="id">The unique identifier for the block type.</param>
    /// <param name="name">The name of the block type.</param>
    public BlockType(ushort id, string name)
    {
        Id = id;
        Name = name;
    }

    public void SetTexture(BlockFace face, string assetName, int index)
    {
        TextureSet.SetTexture(face, assetName, index);
    }

    public void SetTop(string assetName, int index)
    {
        TextureSet.Top = new BlockTextureObject(assetName, index);
    }

    public void SetBottom(string assetName, int index)
    {
        TextureSet.Bottom = new BlockTextureObject(assetName, index);
    }

    public void SetLeft(string assetName, int index)
    {
        TextureSet.Left = new BlockTextureObject(assetName, index);
    }

    public void SetRight(string assetName, int index)
    {
        TextureSet.Right = new BlockTextureObject(assetName, index);
    }

    public void SetFront(string assetName, int index)
    {
        TextureSet.Front = new BlockTextureObject(assetName, index);
    }

    public void SetBack(string assetName, int index)
    {
        TextureSet.Back = new BlockTextureObject(assetName, index);
    }
}
