using System.Numerics;
using Lilly.Engine.Interfaces.Services;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Types;
using Lilly.Voxel.Plugin.Vertexs;
using Serilog;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Services;

/// <summary>
/// Builds optimized mesh data for voxel chunks using a greedy meshing algorithm.
/// Generates vertex/index buffers organized by render type (solid, billboard, item, fluid).
/// </summary>
public sealed class ChunkMeshBuilder
{
    private static readonly BlockFace[] AllFaces =
    [
        BlockFace.Top, BlockFace.Bottom, BlockFace.Front, BlockFace.Back, BlockFace.Left, BlockFace.Right
    ];

    private readonly IBlockRegistry _blockRegistry;
    private readonly ChunkLightingService _lightingService;
    private readonly IAssetManager _assetManager;
    private readonly ILogger _logger = Log.ForContext<ChunkMeshBuilder>();

    [ThreadStatic] private static MeshBuilderContext? _threadContext;

    public ChunkMeshBuilder(
        IBlockRegistry blockRegistry,
        ChunkLightingService lightingService,
        IAssetManager assetManager
    )
    {
        _blockRegistry = blockRegistry;
        _lightingService = lightingService;
        _assetManager = assetManager;
    }

    /// <summary>
    /// Builds mesh data for a chunk synchronously.
    /// </summary>
    /// <param name="chunk">The chunk entity to build mesh for.</param>
    /// <param name="getNeighborChunk">Optional function to retrieve neighbor chunks for face culling (expects chunk coordinates).</param>
    /// <returns>Complete mesh data with all geometry types.</returns>
    public ChunkMeshData BuildMeshData(ChunkEntity chunk, Func<Vector3, ChunkEntity?>? getNeighborChunk = null)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        try
        {
            _threadContext ??= new();
            var context = _threadContext;
            context.Clear();

            // Pre-cache block types to avoid repeated registry lookups
            for (var x = 0; x < ChunkEntity.Size; x++)
            {
                for (var y = 0; y < ChunkEntity.Height; y++)
                {
                    for (var z = 0; z < ChunkEntity.Size; z++)
                    {
                        var blockId = chunk.GetBlockFast(x, y, z);
                        context.BlockTypeCache[x, y, z] = blockId == 0 ? null : _blockRegistry.GetById(blockId);
                    }
                }
            }

            var neighbors = BuildNeighborCache(chunk, getNeighborChunk);

            // Process non-solid geometry first (billboards, items, fluids)
            ProcessSpecialGeometry(chunk, neighbors, context);

            // Process solid geometry with greedy meshing
            BuildSolidFacesGreedy(chunk, neighbors, context);

            return new()
            {
                Vertices = context.SolidVertices.ToArray(),
                Indices = context.SolidIndices.ToArray(),
                TextureHandle = context.SolidTextureHandle,
                SolidAtlasName = context.SolidAtlasName,
                BillboardVertices = context.BillboardVertices.ToArray(),
                BillboardIndices = context.BillboardIndices.ToArray(),
                BillboardTextureHandle = context.BillboardTextureHandle,
                BillboardAtlasName = context.BillboardAtlasName,
                ItemVertices = context.ItemVertices.ToArray(),
                ItemIndices = context.ItemIndices.ToArray(),
                ItemTextureHandle = context.ItemTextureHandle,
                ItemAtlasName = context.ItemAtlasName,
                FluidVertices = context.FluidVertices.ToArray(),
                FluidIndices = context.FluidIndices.ToArray(),
                FluidTextureHandle = context.FluidTextureHandle,
                FluidAtlasName = context.FluidAtlasName
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to build chunk mesh data");

            return new();
        }
    }

    private void AppendBillboard(
        ChunkEntity chunk,
        int x,
        int y,
        int z,
        Vector3 origin,
        Vector3 blockCoord,
        BlockType blockType,
        MeshBuilderContext context
    )
    {
        const float scale = 1f;
        var half = scale * 0.5f;
        var baseHeight = origin.Y;
        var topHeight = baseHeight + scale;
        var centerX = origin.X + half;
        var centerZ = origin.Z + half;

        var lighting = _lightingService.CalculateFaceColor(chunk, x, y, z, BlockFace.Top);
        var lightColor = new Color4b((byte)lighting.X, (byte)lighting.Y, (byte)lighting.Z);

        var bottomA0 = new Vector3(centerX - half, baseHeight, centerZ - half);
        var bottomA1 = new Vector3(centerX + half, baseHeight, centerZ + half);
        var topA1 = new Vector3(bottomA1.X, topHeight, bottomA1.Z);
        var topA0 = new Vector3(bottomA0.X, topHeight, bottomA0.Z);

        var bottomB0 = new Vector3(centerX - half, baseHeight, centerZ + half);
        var bottomB1 = new Vector3(centerX + half, baseHeight, centerZ - half);
        var topB1 = new Vector3(bottomB1.X, topHeight, bottomB1.Z);
        var topB0 = new Vector3(bottomB0.X, topHeight, bottomB0.Z);

        AppendBillboardQuad(
            context.BillboardVertices,
            context.BillboardIndices,
            bottomA0,
            bottomA1,
            topA1,
            topA0,
            blockCoord,
            lightColor,
            blockType,
            context
        );
        AppendBillboardQuad(
            context.BillboardVertices,
            context.BillboardIndices,
            bottomB0,
            bottomB1,
            topB1,
            topB0,
            blockCoord,
            lightColor,
            blockType,
            context
        );
    }

    private void AppendBillboardQuad(
        List<ChunkVertex> vertices,
        List<int> indices,
        Vector3 bottomLeft,
        Vector3 bottomRight,
        Vector3 topRight,
        Vector3 topLeft,
        Vector3 blockCoord,
        Color4b color,
        BlockType blockType,
        MeshBuilderContext context
    )
    {
        var baseIndex = vertices.Count;

        var blockTexture = blockType.TextureSet.GetTextureForFace(BlockFace.Front);
        var (tileBase, tileSize) = GetAtlasRegionForTexture(blockTexture, ref context.BillboardTextureHandle, ref context.BillboardAtlasName);

        var t0 = new Vector2(0f, 1f);
        var t1 = new Vector2(1f, 1f);
        var t2 = new Vector2(1f, 0f);
        var t3 = new Vector2(0f, 0f);

        t0.Y = 1f - t0.Y;
        t1.Y = 1f - t1.Y;
        t2.Y = 1f - t2.Y;
        t3.Y = 1f - t3.Y;

        vertices.Add(new(bottomLeft, color, t0, tileBase, tileSize, blockCoord));
        vertices.Add(new(bottomRight, color, t1, tileBase, tileSize, blockCoord));
        vertices.Add(new(topRight, color, t2, tileBase, tileSize, blockCoord));
        vertices.Add(new(topLeft, color, t3, tileBase, tileSize, blockCoord));

        indices.Add(baseIndex);
        indices.Add(baseIndex + 1);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 3);
    }

    private void AppendFluidBlock(
        ChunkEntity chunk,
        in NeighborChunkCache neighbors,
        int x,
        int y,
        int z,
        Vector3 origin,
        BlockType blockType,
        MeshBuilderContext context
    )
    {
        foreach (var face in AllFaces)
        {
            if (!ShouldRenderFluidFace(chunk, neighbors, x, y, z, face))
            {
                continue;
            }

            var lighting = _lightingService.CalculateFaceColor(chunk, x, y, z, face);
            var color = new Color4b((byte)lighting.X, (byte)lighting.Y, (byte)lighting.Z, 200);
            AppendFluidFace(context.FluidVertices, context.FluidIndices, origin, face, color, blockType, context);
        }
    }

    private void AppendFluidFace(
        List<ChunkFluidVertex> vertices,
        List<int> indices,
        Vector3 origin,
        BlockFace face,
        Color4b color,
        BlockType blockType,
        MeshBuilderContext context
    )
    {
        const float scale = 1f;
        var baseIndex = vertices.Count;

        var blockTexture = blockType.TextureSet.GetTextureForFace(face);
        var (tileBase, tileSize) = GetAtlasRegionForTexture(blockTexture, ref context.FluidTextureHandle, ref context.FluidAtlasName);

        Vector3 v0,
                v1,
                v2,
                v3;
        Vector2 t0,
                t1,
                t2,
                t3;

        switch (face)
        {
            case BlockFace.Top:
                v0 = new(origin.X, origin.Y + scale, origin.Z);
                v1 = new(origin.X, origin.Y + scale, origin.Z + scale);
                v2 = new(origin.X + scale, origin.Y + scale, origin.Z + scale);
                v3 = new(origin.X + scale, origin.Y + scale, origin.Z);

                break;
            case BlockFace.Bottom:
                v0 = new(origin.X, origin.Y, origin.Z + scale);
                v1 = new(origin.X, origin.Y, origin.Z);
                v2 = new(origin.X + scale, origin.Y, origin.Z);
                v3 = new(origin.X + scale, origin.Y, origin.Z + scale);

                break;
            case BlockFace.Front:
                v0 = new(origin.X, origin.Y, origin.Z + scale);
                v1 = new(origin.X, origin.Y + scale, origin.Z + scale);
                v2 = new(origin.X + scale, origin.Y + scale, origin.Z + scale);
                v3 = new(origin.X + scale, origin.Y, origin.Z + scale);

                break;
            case BlockFace.Back:
                v0 = new(origin.X + scale, origin.Y, origin.Z);
                v1 = new(origin.X + scale, origin.Y + scale, origin.Z);
                v2 = new(origin.X, origin.Y + scale, origin.Z);
                v3 = new(origin.X, origin.Y, origin.Z);

                break;
            case BlockFace.Right:
                v0 = new(origin.X + scale, origin.Y, origin.Z + scale);
                v1 = new(origin.X + scale, origin.Y + scale, origin.Z + scale);
                v2 = new(origin.X + scale, origin.Y + scale, origin.Z);
                v3 = new(origin.X + scale, origin.Y, origin.Z);

                break;
            case BlockFace.Left:
                v0 = new(origin.X, origin.Y, origin.Z);
                v1 = new(origin.X, origin.Y + scale, origin.Z);
                v2 = new(origin.X, origin.Y + scale, origin.Z + scale);
                v3 = new(origin.X, origin.Y, origin.Z + scale);

                break;
            default:
                return;
        }

        t0 = new(0f, 0f);
        t1 = new(0f, 1f);
        t2 = new(1f, 1f);
        t3 = new(1f, 0f);

        var direction = (float)GetDirectionIndex(face);
        var isTop = face == BlockFace.Top ? 1f : 0f;

        vertices.Add(new(v0, color, t0, tileBase, tileSize, direction, isTop));
        vertices.Add(new(v1, color, t1, tileBase, tileSize, direction, isTop));
        vertices.Add(new(v2, color, t2, tileBase, tileSize, direction, isTop));
        vertices.Add(new(v3, color, t3, tileBase, tileSize, direction, isTop));

        indices.Add(baseIndex);
        indices.Add(baseIndex + 1);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 3);
    }

    private void AppendGreedyFace(
        List<ChunkVertex> vertices,
        List<int> indices,
        BlockFace face,
        Vector3 blockCoord,
        Color4b lighting,
        int spanU,
        int spanV,
        BlockType blockType,
        MeshBuilderContext context
    )
    {
        const float scale = 1f;
        var origin = blockCoord * scale;
        var spanUScaled = spanU * scale;
        var spanVScaled = spanV * scale;

        Vector3 v0,
                v1,
                v2,
                v3;
        Vector2 t0,
                t1,
                t2,
                t3;

        switch (face)
        {
            case BlockFace.Top:
                v0 = new(origin.X, origin.Y + scale, origin.Z);
                v1 = new(origin.X, origin.Y + scale, origin.Z + spanVScaled);
                v2 = new(origin.X + spanUScaled, origin.Y + scale, origin.Z + spanVScaled);
                v3 = new(origin.X + spanUScaled, origin.Y + scale, origin.Z);
                t0 = new(0f, 0f);
                t1 = new(0f, spanV);
                t2 = new(spanU, spanV);
                t3 = new(spanU, 0f);

                break;

            case BlockFace.Bottom:
                v0 = new(origin.X, origin.Y, origin.Z + spanVScaled);
                v1 = new(origin.X, origin.Y, origin.Z);
                v2 = new(origin.X + spanUScaled, origin.Y, origin.Z);
                v3 = new(origin.X + spanUScaled, origin.Y, origin.Z + spanVScaled);
                t0 = new(0f, 0f);
                t1 = new(0f, spanV);
                t2 = new(spanU, spanV);
                t3 = new(spanU, 0f);

                break;

            case BlockFace.Front:
                v0 = new(origin.X, origin.Y, origin.Z + scale);
                v1 = new(origin.X, origin.Y + spanVScaled, origin.Z + scale);
                v2 = new(origin.X + spanUScaled, origin.Y + spanVScaled, origin.Z + scale);
                v3 = new(origin.X + spanUScaled, origin.Y, origin.Z + scale);
                t0 = new(0f, spanV);
                t1 = new(0f, 0f);
                t2 = new(spanU, 0f);
                t3 = new(spanU, spanV);

                break;

            case BlockFace.Back:
                v0 = new(origin.X + spanUScaled, origin.Y, origin.Z);
                v1 = new(origin.X + spanUScaled, origin.Y + spanVScaled, origin.Z);
                v2 = new(origin.X, origin.Y + spanVScaled, origin.Z);
                v3 = new(origin.X, origin.Y, origin.Z);
                t0 = new(0f, spanV);
                t1 = new(0f, 0f);
                t2 = new(spanU, 0f);
                t3 = new(spanU, spanV);

                break;

            case BlockFace.Right:
                v0 = new(origin.X + scale, origin.Y, origin.Z + spanUScaled);
                v1 = new(origin.X + scale, origin.Y + spanVScaled, origin.Z + spanUScaled);
                v2 = new(origin.X + scale, origin.Y + spanVScaled, origin.Z);
                v3 = new(origin.X + scale, origin.Y, origin.Z);
                t0 = new(0f, spanV);
                t1 = new(0f, 0f);
                t2 = new(spanU, 0f);
                t3 = new(spanU, spanV);

                break;

            case BlockFace.Left:
                v0 = new(origin.X, origin.Y, origin.Z);
                v1 = new(origin.X, origin.Y + spanVScaled, origin.Z);
                v2 = new(origin.X, origin.Y + spanVScaled, origin.Z + spanUScaled);
                v3 = new(origin.X, origin.Y, origin.Z + spanUScaled);
                t0 = new(0f, spanV);
                t1 = new(0f, 0f);
                t2 = new(spanU, 0f);
                t3 = new(spanU, spanV);

                break;

            default:
                return;
        }

        // Flip Y coordinates for OpenGL
        t0.Y = 1f - t0.Y;
        t1.Y = 1f - t1.Y;
        t2.Y = 1f - t2.Y;
        t3.Y = 1f - t3.Y;

        var blockTexture = blockType.TextureSet.GetTextureForFace(face);
        var (tileBase, tileSize) = GetAtlasRegionForTexture(blockTexture, ref context.SolidTextureHandle, ref context.SolidAtlasName);
        var baseIndex = vertices.Count;

        vertices.Add(new(v0, lighting, t0, tileBase, tileSize, blockCoord));
        vertices.Add(new(v1, lighting, t1, tileBase, tileSize, blockCoord));
        vertices.Add(new(v2, lighting, t2, tileBase, tileSize, blockCoord));
        vertices.Add(new(v3, lighting, t3, tileBase, tileSize, blockCoord));

        if (face is BlockFace.Top or BlockFace.Bottom)
        {
            indices.Add(baseIndex);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }
        else
        {
            indices.Add(baseIndex);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 3);
            indices.Add(baseIndex + 2);
        }
    }

    private void AppendItemBillboard(
        ChunkEntity chunk,
        int x,
        int y,
        int z,
        Vector3 origin,
        BlockType blockType,
        MeshBuilderContext context
    )
    {
        const float scale = 1f;
        var center = origin + new Vector3(scale * 0.5f, scale * 0.5f, scale * 0.5f);
        var halfWidth = scale * 0.5f;
        var halfHeight = scale * 0.5f;

        var lighting = _lightingService.CalculateFaceColor(chunk, x, y, z, BlockFace.Top);
        var color = new Color4b((byte)lighting.X, (byte)lighting.Y, (byte)lighting.Z);
        var baseIndex = context.ItemVertices.Count;

        var blockTexture = blockType.TextureSet.GetTextureForFace(BlockFace.Front);
        var (tileBase, tileSize) = GetAtlasRegionForTexture(blockTexture, ref context.ItemTextureHandle, ref context.ItemAtlasName);

        Span<Vector2> offsets = stackalloc Vector2[4]
        {
            new(-halfWidth, halfHeight),
            new(halfWidth, halfHeight),
            new(halfWidth, -halfHeight),
            new(-halfWidth, -halfHeight)
        };

        context.ItemVertices.Add(new(center, color, new(0f, 1f), offsets[0], tileBase, tileSize));
        context.ItemVertices.Add(new(center, color, new(1f, 1f), offsets[1], tileBase, tileSize));
        context.ItemVertices.Add(new(center, color, new(1f, 0f), offsets[2], tileBase, tileSize));
        context.ItemVertices.Add(new(center, color, new(0f, 0f), offsets[3], tileBase, tileSize));

        context.ItemIndices.Add(baseIndex);
        context.ItemIndices.Add(baseIndex + 1);
        context.ItemIndices.Add(baseIndex + 2);
        context.ItemIndices.Add(baseIndex);
        context.ItemIndices.Add(baseIndex + 2);
        context.ItemIndices.Add(baseIndex + 3);
    }

    /// <summary>
    /// Pulls all neighboring chunks once so face culling at chunk boundaries avoids repeated lookups.
    /// </summary>
    private NeighborChunkCache BuildNeighborCache(
        ChunkEntity chunk,
        Func<Vector3, ChunkEntity?>? getNeighborChunk
    )
    {
        if (getNeighborChunk == null)
        {
            return default;
        }

        var coords = chunk.ChunkCoordinates;

        return new(
            getNeighborChunk(coords + new Vector3(-1, 0, 0)),
            getNeighborChunk(coords + new Vector3(1, 0, 0)),
            getNeighborChunk(coords + new Vector3(0, -1, 0)),
            getNeighborChunk(coords + new Vector3(0, 1, 0)),
            getNeighborChunk(coords + new Vector3(0, 0, -1)),
            getNeighborChunk(coords + new Vector3(0, 0, 1))
        );
    }

    /// <summary>
    /// Builds solid block geometry using greedy meshing algorithm.
    /// </summary>
    private void BuildSolidFacesGreedy(
        ChunkEntity chunk,
        in NeighborChunkCache neighbors,
        MeshBuilderContext context
    )
    {
        ProcessHorizontalFaces(chunk, neighbors, BlockFace.Top, context);
        ProcessHorizontalFaces(chunk, neighbors, BlockFace.Bottom, context);
        ProcessDepthAlignedFaces(chunk, neighbors, BlockFace.Front, context);
        ProcessDepthAlignedFaces(chunk, neighbors, BlockFace.Back, context);
        ProcessWidthAlignedFaces(chunk, neighbors, BlockFace.Right, context);
        ProcessWidthAlignedFaces(chunk, neighbors, BlockFace.Left, context);
    }

    private Color4b CalculateLighting(ChunkEntity chunk, int x, int y, int z, BlockFace face)
    {
        var lighting = _lightingService.CalculateFaceColor(chunk, x, y, z, face);

        return new((byte)lighting.X, (byte)lighting.Y, (byte)lighting.Z, (byte)lighting.W);
    }

    private static bool CanMerge(FaceRenderInfo a, FaceRenderInfo b)
    {
        if (!a.HasValue || !b.HasValue)
        {
            return false;
        }

        if (a.BlockType == null || b.BlockType == null)
        {
            return false;
        }

        if (a.BlockType.Id != b.BlockType.Id)
        {
            return false;
        }

        return a.Lighting.Equals(b.Lighting);
    }

    /// <summary>
    /// The core greedy meshing algorithm - merges adjacent faces with same properties.
    /// </summary>
    private void EmitMaskLayer(
        FaceRenderInfo[] mask,
        int width,
        int height,
        BlockFace face,
        List<ChunkVertex> vertices,
        List<int> indices,
        MeshBuilderContext context
    )
    {
        for (var v = 0; v < height; v++)
        {
            var rowStart = v * width;

            for (var u = 0; u < width;)
            {
                var info = mask[rowStart + u];

                if (!info.HasValue)
                {
                    u++;

                    continue;
                }

                var spanU = 1;

                while (u + spanU < width && CanMerge(info, mask[rowStart + u + spanU]))
                {
                    spanU++;
                }

                var spanV = 1;

                while (v + spanV < height)
                {
                    var canExtend = true;
                    var nextRowStart = (v + spanV) * width + u;

                    for (var du = 0; du < spanU; du++)
                    {
                        if (!CanMerge(info, mask[nextRowStart + du]))
                        {
                            canExtend = false;

                            break;
                        }
                    }

                    if (!canExtend)
                    {
                        break;
                    }

                    spanV++;
                }

                var blockCoord = new Vector3(info.X, info.Y, info.Z);
                AppendGreedyFace(vertices, indices, face, blockCoord, info.Lighting, spanU, spanV, info.BlockType, context);

                for (var dv = 0; dv < spanV; dv++)
                {
                    var clearStart = (v + dv) * width + u;

                    for (var du = 0; du < spanU; du++)
                    {
                        mask[clearStart + du] = default;
                    }
                }

                u += spanU;
            }
        }
    }

    private (Vector2 Position, Vector2 Size) GetAtlasRegionForTexture(
        BlockTextureObject texture,
        ref uint textureHandle,
        ref string atlasName
    )
    {
        try
        {
            var region = _assetManager.GetAtlasRegion(texture.AtlasName, texture.Index);

            if (textureHandle == 0)
            {
                try
                {
                    textureHandle = _assetManager.GetTextureHandle(texture.AtlasName + "_atlas");
                }
                catch (Exception handleEx)
                {
                    _logger.Debug(handleEx, "Unable to resolve texture handle for atlas {Atlas}", texture.AtlasName);
                }
            }

            if (string.IsNullOrEmpty(atlasName))
            {
                atlasName = texture.AtlasName;
            }

            return (new(region.Position.X, region.Position.Y), new(region.Size.X, region.Size.Y));
        }
        catch (Exception ex)
        {
            _logger.Warning(
                ex,
                "Failed to get atlas region for texture {AtlasName}:{Index}, using default",
                texture.AtlasName,
                texture.Index
            );

            return (Vector2.Zero, Vector2.One);
        }
    }

    private static int GetDirectionIndex(BlockFace face)
        => face switch
        {
            BlockFace.Front  => 0,
            BlockFace.Back   => 1,
            BlockFace.Right  => 2,
            BlockFace.Left   => 3,
            BlockFace.Top    => 4,
            BlockFace.Bottom => 5,
            _                => 6
        };

    private void ProcessDepthAlignedFaces(
        ChunkEntity chunk,
        in NeighborChunkCache neighbors,
        BlockFace face,
        MeshBuilderContext context
    )
    {
        var width = ChunkEntity.Size;
        var height = ChunkEntity.Height;

        for (var z = 0; z < ChunkEntity.Size; z++)
        {
            Array.Clear(context.MaskBuffer, 0, width * height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var blockType = context.BlockTypeCache[x, y, z];

                    if (blockType == null || blockType == _blockRegistry.Air)
                    {
                        continue;
                    }

                    if (blockType.IsBillboard || blockType.RenderType is BlockRenderType.Item or BlockRenderType.Fluid)
                    {
                        continue;
                    }

                    if (!ShouldRenderFace(chunk, neighbors, x, y, z, face))
                    {
                        continue;
                    }

                    var lighting = CalculateLighting(chunk, x, y, z, face);
                    context.MaskBuffer[x + y * width] = new(x, y, z, lighting, blockType);
                }
            }

            EmitMaskLayer(context.MaskBuffer, width, height, face, context.SolidVertices, context.SolidIndices, context);
        }
    }

    private void ProcessHorizontalFaces(
        ChunkEntity chunk,
        in NeighborChunkCache neighbors,
        BlockFace face,
        MeshBuilderContext context
    )
    {
        var width = ChunkEntity.Size;
        var depth = ChunkEntity.Size;

        for (var y = 0; y < ChunkEntity.Height; y++)
        {
            Array.Clear(context.MaskBuffer, 0, width * depth);

            for (var z = 0; z < depth; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    var blockType = context.BlockTypeCache[x, y, z];

                    if (blockType == null || blockType == _blockRegistry.Air)
                    {
                        continue;
                    }

                    if (blockType.IsBillboard || blockType.RenderType is BlockRenderType.Item or BlockRenderType.Fluid)
                    {
                        continue;
                    }

                    if (!ShouldRenderFace(chunk, neighbors, x, y, z, face))
                    {
                        continue;
                    }

                    var lighting = CalculateLighting(chunk, x, y, z, face);
                    context.MaskBuffer[x + z * width] = new(x, y, z, lighting, blockType);
                }
            }

            EmitMaskLayer(context.MaskBuffer, width, depth, face, context.SolidVertices, context.SolidIndices, context);
        }
    }

    /// <summary>
    /// Processes special geometry types: billboards, items, and fluids.
    /// </summary>
    private void ProcessSpecialGeometry(
        ChunkEntity chunk,
        in NeighborChunkCache neighbors,
        MeshBuilderContext context
    )
    {
        for (var y = 0; y < ChunkEntity.Height; y++)
        {
            for (var z = 0; z < ChunkEntity.Size; z++)
            {
                for (var x = 0; x < ChunkEntity.Size; x++)
                {
                    var blockType = context.BlockTypeCache[x, y, z];

                    if (blockType == null || blockType == _blockRegistry.Air)
                    {
                        continue;
                    }

                    var origin = new Vector3(x, y, z);
                    var blockCoord = new Vector3(x, y, z);

                    if (blockType.IsBillboard)
                    {
                        AppendBillboard(chunk, x, y, z, origin, blockCoord, blockType, context);

                        continue;
                    }

                    if (blockType.RenderType == BlockRenderType.Item)
                    {
                        AppendItemBillboard(chunk, x, y, z, origin, blockType, context);

                        continue;
                    }

                    if (blockType.RenderType == BlockRenderType.Fluid)
                    {
                        AppendFluidBlock(chunk, neighbors, x, y, z, origin, blockType, context);
                    }
                }
            }
        }
    }

    private void ProcessWidthAlignedFaces(
        ChunkEntity chunk,
        in NeighborChunkCache neighbors,
        BlockFace face,
        MeshBuilderContext context
    )
    {
        var depth = ChunkEntity.Size;
        var height = ChunkEntity.Height;

        for (var x = 0; x < ChunkEntity.Size; x++)
        {
            Array.Clear(context.MaskBuffer, 0, depth * height);

            for (var y = 0; y < height; y++)
            {
                for (var z = 0; z < depth; z++)
                {
                    var blockType = context.BlockTypeCache[x, y, z];

                    if (blockType == null || blockType == _blockRegistry.Air)
                    {
                        continue;
                    }

                    if (blockType.IsBillboard || blockType.RenderType is BlockRenderType.Item or BlockRenderType.Fluid)
                    {
                        continue;
                    }

                    if (!ShouldRenderFace(chunk, neighbors, x, y, z, face))
                    {
                        continue;
                    }

                    var lighting = CalculateLighting(chunk, x, y, z, face);
                    context.MaskBuffer[z + y * depth] = new(x, y, z, lighting, blockType);
                }
            }

            EmitMaskLayer(context.MaskBuffer, depth, height, face, context.SolidVertices, context.SolidIndices, context);
        }
    }

    private bool ShouldRenderFace(
        ChunkEntity chunk,
        in NeighborChunkCache neighbors,
        int x,
        int y,
        int z,
        BlockFace face
    )
    {
        if (chunk.TryGetAdjacentBlock(x, y, z, face, out var neighborId))
        {
            if (neighborId == 0)
            {
                return true;
            }

            var neighborType = _blockRegistry.GetById(neighborId);

            return !neighborType.IsSolid ||
                   neighborType.IsTransparent ||
                   neighborType.IsBillboard ||
                   neighborType.RenderType == BlockRenderType.Item;
        }

        if (TryGetNeighborBlock(neighbors, x, y, z, face, out neighborId))
        {
            if (neighborId == 0)
            {
                return true;
            }

            var neighborType = _blockRegistry.GetById(neighborId);

            return !neighborType.IsSolid ||
                   neighborType.IsTransparent ||
                   neighborType.IsBillboard ||
                   neighborType.RenderType == BlockRenderType.Item;
        }

        return true;
    }

    private bool ShouldRenderFluidFace(
        ChunkEntity chunk,
        in NeighborChunkCache neighbors,
        int x,
        int y,
        int z,
        BlockFace face
    )
    {
        if (chunk.TryGetAdjacentBlock(x, y, z, face, out var neighborId))
        {
            if (neighborId == 0)
            {
                return true;
            }

            var neighborType = _blockRegistry.GetById(neighborId);

            return neighborType.RenderType != BlockRenderType.Fluid && (!neighborType.IsSolid || neighborType.IsTransparent);
        }

        if (TryGetNeighborBlock(neighbors, x, y, z, face, out neighborId))
        {
            if (neighborId == 0)
            {
                return true;
            }

            var neighborType = _blockRegistry.GetById(neighborId);

            return neighborType.RenderType != BlockRenderType.Fluid && (!neighborType.IsSolid || neighborType.IsTransparent);
        }

        return true;
    }

    private bool TryGetNeighborBlock(
        in NeighborChunkCache neighbors,
        int x,
        int y,
        int z,
        BlockFace face,
        out ushort blockId
    )
    {
        blockId = 0;
        var neighborChunk = neighbors.Get(face);

        if (neighborChunk == null)
        {
            return false;
        }

        var localX = x;
        var localY = y;
        var localZ = z;

        switch (face)
        {
            case BlockFace.Left:
                localX = ChunkEntity.Size - 1;

                break;
            case BlockFace.Right:
                localX = 0;

                break;
            case BlockFace.Bottom:
                localY = ChunkEntity.Height - 1;

                break;
            case BlockFace.Top:
                localY = 0;

                break;
            case BlockFace.Back:
                localZ = ChunkEntity.Size - 1;

                break;
            case BlockFace.Front:
                localZ = 0;

                break;
            default:
                return false;
        }

        blockId = neighborChunk.GetBlock(localX, localY, localZ);

        return true;
    }
}
