using Lilly.Voxel.Plugin.Blocks;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Services;

/// <summary>
/// Render info for a single face used during greedy meshing.
/// </summary>
internal readonly struct FaceRenderInfo
{
    public FaceRenderInfo(int x, int y, int z, Color4b lighting, BlockType blockType)
    {
        X = x;
        Y = y;
        Z = z;
        Lighting = lighting;
        BlockType = blockType;
        HasValue = true;
    }

    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    public Color4b Lighting { get; }
    public BlockType BlockType { get; }
    public bool HasValue { get; }
}
