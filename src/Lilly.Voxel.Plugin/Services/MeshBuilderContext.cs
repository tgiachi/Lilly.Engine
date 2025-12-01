using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Vertexs;

namespace Lilly.Voxel.Plugin.Services;

/// <summary>
/// Holds reusable buffers for building chunk meshes on a worker thread.
/// </summary>
internal sealed class MeshBuilderContext
{
    public readonly List<ChunkVertex> SolidVertices = new(8192);
    public readonly List<int> SolidIndices = new(16384);
    public readonly List<ChunkVertex> BillboardVertices = new(2048);
    public readonly List<int> BillboardIndices = new(4096);
    public readonly List<ChunkItemVertex> ItemVertices = new(1024);
    public readonly List<int> ItemIndices = new(2048);
    public readonly List<ChunkFluidVertex> FluidVertices = new(2048);
    public readonly List<int> FluidIndices = new(4096);
    public readonly FaceRenderInfo[] MaskBuffer;
    public readonly BlockType?[,,] BlockTypeCache;

    public uint SolidTextureHandle;
    public uint BillboardTextureHandle;
    public uint ItemTextureHandle;
    public uint FluidTextureHandle;

    public MeshBuilderContext()
    {
        int maxFaceSize = Math.Max(ChunkEntity.Size * ChunkEntity.Size, ChunkEntity.Size * ChunkEntity.Height);
        MaskBuffer = new FaceRenderInfo[maxFaceSize];
        BlockTypeCache = new BlockType[ChunkEntity.Size, ChunkEntity.Height, ChunkEntity.Size];
    }

    public void Clear()
    {
        SolidVertices.Clear();
        SolidIndices.Clear();
        BillboardVertices.Clear();
        BillboardIndices.Clear();
        ItemVertices.Clear();
        ItemIndices.Clear();
        FluidVertices.Clear();
        FluidIndices.Clear();
        Array.Clear(MaskBuffer, 0, MaskBuffer.Length);
        Array.Clear(BlockTypeCache, 0, BlockTypeCache.Length);
        SolidTextureHandle = 0;
        BillboardTextureHandle = 0;
        ItemTextureHandle = 0;
        FluidTextureHandle = 0;
    }
}
