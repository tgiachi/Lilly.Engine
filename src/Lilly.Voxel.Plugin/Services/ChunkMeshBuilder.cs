using System;
using System.Collections.Generic;
using Silk.NET.Maths;
using Serilog;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Primitives.Vertex;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Services;

/// <summary>
/// Builds optimized mesh data for voxel chunks using greedy meshing algorithm.
/// This service takes block data from a ChunkEntity and generates vertex/index buffers
/// organized by render type (solid, billboard, item, fluid).
/// </summary>
public sealed class ChunkMeshBuilder
{
    private static readonly BlockFace[] AllFaces =
        [BlockFace.Top, BlockFace.Bottom, BlockFace.Front, BlockFace.Back, BlockFace.Left, BlockFace.Right];

    private readonly IBlockRegistry _blockRegistry;
    private readonly ChunkLightingService _lightingService;
    private readonly IAssetManager _assetManager;
    private readonly ILogger _logger = Log.ForContext<ChunkMeshBuilder>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkMeshBuilder"/> class.
    /// </summary>
    /// <param name="blockRegistry">Block registry for resolving block properties.</param>
    /// <param name="lightingService">Lighting service for ambient occlusion calculations.</param>
    /// <param name="assetManager">Asset manager for loading texture atlases.</param>
    public ChunkMeshBuilder(IBlockRegistry blockRegistry, ChunkLightingService lightingService, IAssetManager assetManager)
    {
        _blockRegistry = blockRegistry;
        _lightingService = lightingService;
        _assetManager = assetManager;
    }

    /// <summary>
    /// Builds mesh data for a chunk synchronously.
    /// </summary>
    /// <param name="chunk">The chunk entity to build mesh for.</param>
    /// <param name="getNeighborChunk">Optional function to retrieve neighbor chunks for face culling.</param>
    /// <returns>Complete mesh data with all geometry types.</returns>
    public ChunkMeshData BuildMeshData(ChunkEntity chunk, Func<ChunkCoordinates, ChunkEntity?>? getNeighborChunk = null)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        try
        {
            var solidVertices = new List<ChunkVertex>(8192);
            var solidIndices = new List<int>(16384);

            var billboardVertices = new List<ChunkVertex>(2048);
            var billboardIndices = new List<int>(4096);

            var itemVertices = new List<ChunkItemVertex>(1024);
            var itemIndices = new List<int>(2048);

            var fluidVertices = new List<ChunkFluidVertex>(2048);
            var fluidIndices = new List<int>(4096);

            // Process non-solid geometry first (billboards, items, fluids)
            ProcessSpecialGeometry(
                chunk,
                getNeighborChunk,
                billboardVertices,
                billboardIndices,
                itemVertices,
                itemIndices,
                fluidVertices,
                fluidIndices
            );

            // Process solid geometry with greedy meshing
            BuildSolidFacesGreedy(chunk, getNeighborChunk, solidVertices, solidIndices);

            return new ChunkMeshData
            {
                Vertices = solidVertices.ToArray(),
                Indices = solidIndices.ToArray(),
                BillboardVertices = billboardVertices.ToArray(),
                BillboardIndices = billboardIndices.ToArray(),
                ItemVertices = itemVertices.ToArray(),
                ItemIndices = itemIndices.ToArray(),
                FluidVertices = fluidVertices.ToArray(),
                FluidIndices = fluidIndices.ToArray(),
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to build chunk mesh data");

            return new ChunkMeshData();
        }
    }

    /// <summary>
    /// Processes special geometry types: billboards, items, and fluids.
    /// </summary>
    private void ProcessSpecialGeometry(
        ChunkEntity chunk,
        Func<ChunkCoordinates, ChunkEntity?>? getNeighborChunk,
        List<ChunkVertex> billboardVertices,
        List<int> billboardIndices,
        List<ChunkItemVertex> itemVertices,
        List<int> itemIndices,
        List<ChunkFluidVertex> fluidVertices,
        List<int> fluidIndices
    )
    {
        for (int y = 0; y < ChunkEntity.Height; y++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int x = 0; x < ChunkEntity.Size; x++)
                {
                    var blockId = chunk.GetBlock(x, y, z);

                    if (blockId == 0)
                    {
                        continue;
                    }

                    var blockType = _blockRegistry.GetById(blockId);

                    if (blockType == _blockRegistry.Air)
                    {
                        continue;
                    }

                    var origin = new Vector3D<float>(x, y, z);
                    var blockCoord = new Vector3D<float>(x, y, z);

                    // Process billboards (flowers, vegetation)
                    if (blockType.IsBillboard)
                    {
                        AppendBillboard(chunk, x, y, z, origin, blockCoord, blockType, billboardVertices, billboardIndices);

                        continue;
                    }

                    // Process item billboards
                    if (blockType.RenderType == BlockRenderType.Item)
                    {
                        AppendItemBillboard(chunk, x, y, z, origin, blockType, itemVertices, itemIndices);

                        continue;
                    }

                    // Process fluids
                    if (blockType.RenderType == BlockRenderType.Fluid)
                    {
                        AppendFluidBlock(chunk, getNeighborChunk, x, y, z, origin, blockType, fluidVertices, fluidIndices);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Builds solid block geometry using greedy meshing algorithm.
    /// </summary>
    private void BuildSolidFacesGreedy(
        ChunkEntity chunk,
        Func<ChunkCoordinates, ChunkEntity?>? getNeighborChunk,
        List<ChunkVertex> vertices,
        List<int> indices
    )
    {
        ProcessHorizontalFaces(chunk, getNeighborChunk, BlockFace.Top, vertices, indices);
        ProcessHorizontalFaces(chunk, getNeighborChunk, BlockFace.Bottom, vertices, indices);
        ProcessDepthAlignedFaces(chunk, getNeighborChunk, BlockFace.Front, vertices, indices);
        ProcessDepthAlignedFaces(chunk, getNeighborChunk, BlockFace.Back, vertices, indices);
        ProcessWidthAlignedFaces(chunk, getNeighborChunk, BlockFace.Right, vertices, indices);
        ProcessWidthAlignedFaces(chunk, getNeighborChunk, BlockFace.Left, vertices, indices);
    }

    /// <summary>
    /// Processes horizontal faces (Top/Bottom) for greedy meshing.
    /// </summary>
    private void ProcessHorizontalFaces(
        ChunkEntity chunk,
        Func<ChunkCoordinates, ChunkEntity?>? getNeighborChunk,
        BlockFace face,
        List<ChunkVertex> vertices,
        List<int> indices
    )
    {
        var width = ChunkEntity.Size;
        var depth = ChunkEntity.Size;
        var mask = new FaceRenderInfo[width * depth];

        for (int y = 0; y < ChunkEntity.Height; y++)
        {
            Array.Clear(mask);

            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    var blockId = chunk.GetBlock(x, y, z);

                    if (blockId == 0)
                        continue;

                    var blockType = _blockRegistry.GetById(blockId);

                    if (blockType == _blockRegistry.Air)
                        continue;

                    // Skip non-solid geometry
                    if (blockType.IsBillboard || blockType.RenderType is BlockRenderType.Item or BlockRenderType.Fluid)
                        continue;

                    if (!ShouldRenderFace(chunk, getNeighborChunk, x, y, z, face, blockType))
                        continue;

                    var lighting = CalculateLighting(chunk, x, y, z, face);
                    mask[x + z * width] = new FaceRenderInfo(x, y, z, lighting, blockType);
                }
            }

            EmitMaskLayer(mask, width, depth, face, vertices, indices);
        }
    }

    /// <summary>
    /// Processes depth-aligned faces (Front/Back) for greedy meshing.
    /// </summary>
    private void ProcessDepthAlignedFaces(
        ChunkEntity chunk,
        Func<ChunkCoordinates, ChunkEntity?>? getNeighborChunk,
        BlockFace face,
        List<ChunkVertex> vertices,
        List<int> indices
    )
    {
        var width = ChunkEntity.Size;
        var height = ChunkEntity.Height;
        var mask = new FaceRenderInfo[width * height];

        for (int z = 0; z < ChunkEntity.Size; z++)
        {
            Array.Clear(mask);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var blockId = chunk.GetBlock(x, y, z);

                    if (blockId == 0)
                        continue;

                    var blockType = _blockRegistry.GetById(blockId);

                    if (blockType == _blockRegistry.Air)
                        continue;

                    if (blockType.IsBillboard || blockType.RenderType is BlockRenderType.Item or BlockRenderType.Fluid)
                        continue;

                    if (!ShouldRenderFace(chunk, getNeighborChunk, x, y, z, face, blockType))
                        continue;

                    var lighting = CalculateLighting(chunk, x, y, z, face);
                    mask[x + y * width] = new FaceRenderInfo(x, y, z, lighting, blockType);
                }
            }

            EmitMaskLayer(mask, width, height, face, vertices, indices);
        }
    }

    /// <summary>
    /// Processes width-aligned faces (Left/Right) for greedy meshing.
    /// </summary>
    private void ProcessWidthAlignedFaces(
        ChunkEntity chunk,
        Func<ChunkCoordinates, ChunkEntity?>? getNeighborChunk,
        BlockFace face,
        List<ChunkVertex> vertices,
        List<int> indices
    )
    {
        var depth = ChunkEntity.Size;
        var height = ChunkEntity.Height;
        var mask = new FaceRenderInfo[depth * height];

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            Array.Clear(mask);

            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    var blockId = chunk.GetBlock(x, y, z);

                    if (blockId == 0)
                        continue;

                    var blockType = _blockRegistry.GetById(blockId);

                    if (blockType == _blockRegistry.Air)
                        continue;

                    if (blockType.IsBillboard || blockType.RenderType is BlockRenderType.Item or BlockRenderType.Fluid)
                        continue;

                    if (!ShouldRenderFace(chunk, getNeighborChunk, x, y, z, face, blockType))
                        continue;

                    var lighting = CalculateLighting(chunk, x, y, z, face);
                    mask[z + y * depth] = new FaceRenderInfo(x, y, z, lighting, blockType);
                }
            }

            EmitMaskLayer(mask, depth, height, face, vertices, indices);
        }
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
        List<int> indices
    )
    {
        for (int v = 0; v < height; v++)
        {
            int rowStart = v * width;

            for (int u = 0; u < width;)
            {
                var info = mask[rowStart + u];

                if (!info.HasValue)
                {
                    u++;

                    continue;
                }

                // Extend horizontally
                int spanU = 1;

                while (u + spanU < width)
                {
                    if (!CanMerge(info, mask[rowStart + u + spanU]))
                    {
                        break;
                    }
                    spanU++;
                }

                // Extend vertically
                int spanV = 1;

                while (v + spanV < height)
                {
                    bool canExtend = true;
                    int nextRowStart = (v + spanV) * width + u;

                    for (int du = 0; du < spanU; du++)
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

                // Emit the merged quad
                var blockCoord = new Vector3D<float>(info.X, info.Y, info.Z);
                AppendGreedyFace(vertices, indices, face, blockCoord, info.Lighting, spanU, spanV, info.BlockType);

                // Clear the mask to avoid duplicates
                for (int dv = 0; dv < spanV; dv++)
                {
                    int clearStart = (v + dv) * width + u;

                    for (int du = 0; du < spanU; du++)
                    {
                        mask[clearStart + du] = default;
                    }
                }

                u += spanU;
            }
        }
    }

    /// <summary>
    /// Appends a merged quad face with proper scaling and UV mapping.
    /// </summary>
    private void AppendGreedyFace(
        List<ChunkVertex> vertices,
        List<int> indices,
        BlockFace face,
        Vector3D<float> blockCoord,
        Vector4D<byte> lighting,
        int spanU,
        int spanV,
        BlockType blockType
    )
    {
        const float scale = 1f;
        var origin = blockCoord * scale;
        float spanUScaled = spanU * scale;
        float spanVScaled = spanV * scale;

        Vector3D<float> v0,
                        v1,
                        v2,
                        v3;
        Vector2D<float> t0,
                        t1,
                        t2,
                        t3;

        // Generate vertices based on face direction
        switch (face)
        {
            case BlockFace.Top:
                v0 = new Vector3D<float>(origin.X, origin.Y + scale, origin.Z);
                v1 = new Vector3D<float>(origin.X, origin.Y + scale, origin.Z + spanVScaled);
                v2 = new Vector3D<float>(origin.X + spanUScaled, origin.Y + scale, origin.Z + spanVScaled);
                v3 = new Vector3D<float>(origin.X + spanUScaled, origin.Y + scale, origin.Z);
                t0 = new Vector2D<float>(0f, 0f);
                t1 = new Vector2D<float>(0f, spanV);
                t2 = new Vector2D<float>(spanU, spanV);
                t3 = new Vector2D<float>(spanU, 0f);

                break;

            case BlockFace.Bottom:
                v0 = new Vector3D<float>(origin.X, origin.Y, origin.Z + spanVScaled);
                v1 = new Vector3D<float>(origin.X, origin.Y, origin.Z);
                v2 = new Vector3D<float>(origin.X + spanUScaled, origin.Y, origin.Z);
                v3 = new Vector3D<float>(origin.X + spanUScaled, origin.Y, origin.Z + spanVScaled);
                t0 = new Vector2D<float>(0f, 0f);
                t1 = new Vector2D<float>(0f, spanV);
                t2 = new Vector2D<float>(spanU, spanV);
                t3 = new Vector2D<float>(spanU, 0f);

                break;

            case BlockFace.Front:
                v0 = new Vector3D<float>(origin.X, origin.Y, origin.Z + scale);
                v1 = new Vector3D<float>(origin.X, origin.Y + spanVScaled, origin.Z + scale);
                v2 = new Vector3D<float>(origin.X + spanUScaled, origin.Y + spanVScaled, origin.Z + scale);
                v3 = new Vector3D<float>(origin.X + spanUScaled, origin.Y, origin.Z + scale);
                t0 = new Vector2D<float>(0f, spanV);
                t1 = new Vector2D<float>(0f, 0f);
                t2 = new Vector2D<float>(spanU, 0f);
                t3 = new Vector2D<float>(spanU, spanV);

                break;

            case BlockFace.Back:
                v0 = new Vector3D<float>(origin.X + spanUScaled, origin.Y, origin.Z);
                v1 = new Vector3D<float>(origin.X + spanUScaled, origin.Y + spanVScaled, origin.Z);
                v2 = new Vector3D<float>(origin.X, origin.Y + spanVScaled, origin.Z);
                v3 = new Vector3D<float>(origin.X, origin.Y, origin.Z);
                t0 = new Vector2D<float>(0f, spanV);
                t1 = new Vector2D<float>(0f, 0f);
                t2 = new Vector2D<float>(spanU, 0f);
                t3 = new Vector2D<float>(spanU, spanV);

                break;

            case BlockFace.Right:
                v0 = new Vector3D<float>(origin.X + scale, origin.Y, origin.Z + spanUScaled);
                v1 = new Vector3D<float>(origin.X + scale, origin.Y + spanVScaled, origin.Z + spanUScaled);
                v2 = new Vector3D<float>(origin.X + scale, origin.Y + spanVScaled, origin.Z);
                v3 = new Vector3D<float>(origin.X + scale, origin.Y, origin.Z);
                t0 = new Vector2D<float>(0f, spanV);
                t1 = new Vector2D<float>(0f, 0f);
                t2 = new Vector2D<float>(spanU, 0f);
                t3 = new Vector2D<float>(spanU, spanV);

                break;

            case BlockFace.Left:
                v0 = new Vector3D<float>(origin.X, origin.Y, origin.Z);
                v1 = new Vector3D<float>(origin.X, origin.Y + spanVScaled, origin.Z);
                v2 = new Vector3D<float>(origin.X, origin.Y + spanVScaled, origin.Z + spanUScaled);
                v3 = new Vector3D<float>(origin.X, origin.Y, origin.Z + spanUScaled);
                t0 = new Vector2D<float>(0f, spanV);
                t1 = new Vector2D<float>(0f, 0f);
                t2 = new Vector2D<float>(spanU, 0f);
                t3 = new Vector2D<float>(spanU, spanV);

                break;

            default:
                return;
        }

        // Flip Y coordinates for OpenGL
        t0.Y = 1f - t0.Y;
        t1.Y = 1f - t1.Y;
        t2.Y = 1f - t2.Y;
        t3.Y = 1f - t3.Y;

        // Get texture atlas region for this block face
        var blockTexture = blockType.TextureSet.GetTextureForFace(face);
        var (tileBase, tileSize) = GetAtlasRegionForTexture(blockTexture);
        var baseIndex = vertices.Count;

        vertices.Add(new ChunkVertex(v0, lighting, t0, tileBase, tileSize, blockCoord));
        vertices.Add(new ChunkVertex(v1, lighting, t1, tileBase, tileSize, blockCoord));
        vertices.Add(new ChunkVertex(v2, lighting, t2, tileBase, tileSize, blockCoord));
        vertices.Add(new ChunkVertex(v3, lighting, t3, tileBase, tileSize, blockCoord));

        // Generate indices with consistent outward winding (CCW for OpenGL)
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

    /// <summary>
    /// Checks if two face render infos can be merged.
    /// </summary>
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

        return a.Lighting == b.Lighting;
    }

    /// <summary>
    /// Determines if a face should be rendered based on adjacent blocks.
    /// </summary>
    private bool ShouldRenderFace(
        ChunkEntity chunk,
        Func<ChunkCoordinates, ChunkEntity?>? getNeighborChunk,
        int x,
        int y,
        int z,
        BlockFace face,
        BlockType currentType
    )
    {
        // Try to get neighbor block in same chunk
        if (chunk.TryGetAdjacentBlock(x, y, z, face, out var neighborId))
        {
            if (neighborId == 0)
            {
                return true;
            }

            var neighborType = _blockRegistry.GetById(neighborId);

            return !neighborType.IsSolid || neighborType.IsTransparent;
        }

        // Try to get neighbor from adjacent chunk
        if (getNeighborChunk != null && TryGetNeighborBlock(chunk, getNeighborChunk, x, y, z, face, out neighborId))
        {
            if (neighborId == 0)
                return true;

            var neighborType = _blockRegistry.GetById(neighborId);

            return !neighborType.IsSolid || neighborType.IsTransparent;
        }

        // If no neighbor available, render the face (chunk boundary)
        return true;
    }

    /// <summary>
    /// Retrieves a block from a neighboring chunk.
    /// </summary>
    private bool TryGetNeighborBlock(
        ChunkEntity chunk,
        Func<ChunkCoordinates, ChunkEntity?> getNeighborChunk,
        int x,
        int y,
        int z,
        BlockFace face,
        out ushort blockId
    )
    {
        blockId = 0;
        var neighborCoords = chunk.ChunkCoordinates;
        var localX = x;
        var localY = y;
        var localZ = z;

        switch (face)
        {
            case BlockFace.Left:
                neighborCoords += ChunkCoordinates.Left;
                localX = ChunkEntity.Size - 1;

                break;
            case BlockFace.Right:
                neighborCoords += ChunkCoordinates.Right;
                localX = 0;

                break;
            case BlockFace.Bottom:
                neighborCoords += ChunkCoordinates.Down;
                localY = ChunkEntity.Height - 1;

                break;
            case BlockFace.Top:
                neighborCoords += ChunkCoordinates.Up;
                localY = 0;

                break;
            case BlockFace.Back:
                neighborCoords += ChunkCoordinates.Backward;
                localZ = ChunkEntity.Size - 1;

                break;
            case BlockFace.Front:
                neighborCoords += ChunkCoordinates.Forward;
                localZ = 0;

                break;
            default:
                return false;
        }

        var neighborChunk = getNeighborChunk(neighborCoords);

        if (neighborChunk == null)
            return false;

        blockId = neighborChunk.GetBlock(localX, localY, localZ);

        return true;
    }

    /// <summary>
    /// Calculates lighting color for a face using ambient occlusion.
    /// </summary>
    private Vector4D<byte> CalculateLighting(ChunkEntity chunk, int x, int y, int z, BlockFace face)
    {
        return _lightingService.CalculateFaceColor(chunk, x, y, z, face);
    }

    /// <summary>
    /// Gets the atlas region for a block texture object.
    /// </summary>
    private (Vector2D<float> Position, Vector2D<float> Size) GetAtlasRegionForTexture(BlockTextureObject texture)
    {
        try
        {
            var region = _assetManager.GetAtlasRegion(texture.AtlasName, texture.Index);
            return (region.Position, region.Size);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to get atlas region for texture {AtlasName}:{Index}, using default",
                texture.AtlasName, texture.Index);
            return (Vector2D<float>.Zero, Vector2D<float>.One);
        }
    }

    /// <summary>
    /// Gets the direction index for encoding in vertex alpha channel.
    /// </summary>
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

    /// <summary>
    /// Appends a billboard (flower/vegetation) to the vertex list.
    /// </summary>
    private void AppendBillboard(
        ChunkEntity chunk,
        int x,
        int y,
        int z,
        Vector3D<float> origin,
        Vector3D<float> blockCoord,
        BlockType blockType,
        List<ChunkVertex> vertices,
        List<int> indices
    )
    {
        const float scale = 1f;
        var half = scale * 0.5f;
        var baseHeight = origin.Y;
        var topHeight = baseHeight + scale;
        var centerX = origin.X + half;
        var centerZ = origin.Z + half;

        var color = new Vector4D<byte>(255, 255, 255, 6);

        // Create two crossed quads
        var bottomA0 = new Vector3D<float>(centerX - half, baseHeight, centerZ - half);
        var bottomA1 = new Vector3D<float>(centerX + half, baseHeight, centerZ + half);
        var topA1 = new Vector3D<float>(bottomA1.X, topHeight, bottomA1.Z);
        var topA0 = new Vector3D<float>(bottomA0.X, topHeight, bottomA0.Z);

        var bottomB0 = new Vector3D<float>(centerX - half, baseHeight, centerZ + half);
        var bottomB1 = new Vector3D<float>(centerX + half, baseHeight, centerZ - half);
        var topB1 = new Vector3D<float>(bottomB1.X, topHeight, bottomB1.Z);
        var topB0 = new Vector3D<float>(bottomB0.X, topHeight, bottomB0.Z);

        AppendBillboardQuad(vertices, indices, bottomA0, bottomA1, topA1, topA0, blockCoord, color, blockType);
        AppendBillboardQuad(vertices, indices, bottomB0, bottomB1, topB1, topB0, blockCoord, color, blockType);
    }

    /// <summary>
    /// Appends a single billboard quad.
    /// </summary>
    private void AppendBillboardQuad(
        List<ChunkVertex> vertices,
        List<int> indices,
        Vector3D<float> bottomLeft,
        Vector3D<float> bottomRight,
        Vector3D<float> topRight,
        Vector3D<float> topLeft,
        Vector3D<float> blockCoord,
        Vector4D<byte> color,
        BlockType blockType
    )
    {
        var baseIndex = vertices.Count;

        // Get texture atlas region for this billboard
        var blockTexture = blockType.TextureSet.GetTextureForFace(BlockFace.Front);
        var (tileBase, tileSize) = GetAtlasRegionForTexture(blockTexture);

        var t0 = new Vector2D<float>(0f, 1f);
        var t1 = new Vector2D<float>(1f, 1f);
        var t2 = new Vector2D<float>(1f, 0f);
        var t3 = new Vector2D<float>(0f, 0f);

        t0.Y = 1f - t0.Y;
        t1.Y = 1f - t1.Y;
        t2.Y = 1f - t2.Y;
        t3.Y = 1f - t3.Y;

        vertices.Add(new ChunkVertex(bottomLeft, color, t0, tileBase, tileSize, blockCoord));
        vertices.Add(new ChunkVertex(bottomRight, color, t1, tileBase, tileSize, blockCoord));
        vertices.Add(new ChunkVertex(topRight, color, t2, tileBase, tileSize, blockCoord));
        vertices.Add(new ChunkVertex(topLeft, color, t3, tileBase, tileSize, blockCoord));

        indices.Add(baseIndex);
        indices.Add(baseIndex + 1);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 3);
    }

    /// <summary>
    /// Appends an item billboard (drops, etc).
    /// </summary>
    private void AppendItemBillboard(
        ChunkEntity chunk,
        int x,
        int y,
        int z,
        Vector3D<float> origin,
        BlockType blockType,
        List<ChunkItemVertex> vertices,
        List<int> indices
    )
    {
        const float scale = 1f;
        var center = origin + new Vector3D<float>(scale * 0.5f, scale * 0.5f, scale * 0.5f);
        var halfWidth = scale * 0.5f;
        var halfHeight = scale * 0.5f;

        var color = new Vector4D<byte>(255, 255, 255, 255);
        var baseIndex = vertices.Count;

        // Get texture atlas region for this item
        var blockTexture = blockType.TextureSet.GetTextureForFace(BlockFace.Front);
        var (tileBase, tileSize) = GetAtlasRegionForTexture(blockTexture);

        var offsets = new[]
        {
            new Vector2D<float>(-halfWidth, halfHeight),
            new Vector2D<float>(halfWidth, halfHeight),
            new Vector2D<float>(halfWidth, -halfHeight),
            new Vector2D<float>(-halfWidth, -halfHeight)
        };

        vertices.Add(new ChunkItemVertex(center, color, new Vector2D<float>(0f, 1f), offsets[0], tileBase, tileSize));
        vertices.Add(new ChunkItemVertex(center, color, new Vector2D<float>(1f, 1f), offsets[1], tileBase, tileSize));
        vertices.Add(new ChunkItemVertex(center, color, new Vector2D<float>(1f, 0f), offsets[2], tileBase, tileSize));
        vertices.Add(new ChunkItemVertex(center, color, new Vector2D<float>(0f, 0f), offsets[3], tileBase, tileSize));

        indices.Add(baseIndex);
        indices.Add(baseIndex + 1);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 3);
    }

    /// <summary>
    /// Appends a fluid block.
    /// </summary>
    private void AppendFluidBlock(
        ChunkEntity chunk,
        Func<ChunkCoordinates, ChunkEntity?>? getNeighborChunk,
        int x,
        int y,
        int z,
        Vector3D<float> origin,
        BlockType blockType,
        List<ChunkFluidVertex> vertices,
        List<int> indices
    )
    {
        var color = new Vector4D<byte>(100, 150, 255, 200);

        // Render all faces that are adjacent to non-fluid blocks
        foreach (var face in AllFaces)
        {
            if (!ShouldRenderFluidFace(chunk, getNeighborChunk, x, y, z, face))
                continue;

            AppendFluidFace(vertices, indices, origin, face, color, blockType);
        }
    }

    /// <summary>
    /// Checks if a fluid face should be rendered.
    /// </summary>
    private bool ShouldRenderFluidFace(
        ChunkEntity chunk,
        Func<ChunkCoordinates, ChunkEntity?>? getNeighborChunk,
        int x,
        int y,
        int z,
        BlockFace face
    )
    {
        // Try adjacent block in same chunk
        if (chunk.TryGetAdjacentBlock(x, y, z, face, out var neighborId))
        {
            if (neighborId == 0)
                return true;

            var neighborType = _blockRegistry.GetById(neighborId);

            return neighborType.RenderType != BlockRenderType.Fluid && (!neighborType.IsSolid || neighborType.IsTransparent);
        }

        // Try neighbor chunk
        if (getNeighborChunk != null && TryGetNeighborBlock(chunk, getNeighborChunk, x, y, z, face, out neighborId))
        {
            if (neighborId == 0)
                return true;

            var neighborType = _blockRegistry.GetById(neighborId);

            return neighborType.RenderType != BlockRenderType.Fluid && (!neighborType.IsSolid || neighborType.IsTransparent);
        }

        // Missing neighbor chunk, render to prevent holes
        return true;
    }

    /// <summary>
    /// Appends a fluid face.
    /// </summary>
    private void AppendFluidFace(
        List<ChunkFluidVertex> vertices,
        List<int> indices,
        Vector3D<float> origin,
        BlockFace face,
        Vector4D<byte> color,
        BlockType blockType
    )
    {
        const float scale = 1f;
        var baseIndex = vertices.Count;

        // Get texture atlas region for this fluid
        var blockTexture = blockType.TextureSet.GetTextureForFace(face);
        var (tileBase, tileSize) = GetAtlasRegionForTexture(blockTexture);

        Vector3D<float> v0,
                        v1,
                        v2,
                        v3;
        Vector2D<float> t0,
                        t1,
                        t2,
                        t3;

        switch (face)
        {
            case BlockFace.Top:
                v0 = new Vector3D<float>(origin.X, origin.Y + scale, origin.Z);
                v1 = new Vector3D<float>(origin.X, origin.Y + scale, origin.Z + scale);
                v2 = new Vector3D<float>(origin.X + scale, origin.Y + scale, origin.Z + scale);
                v3 = new Vector3D<float>(origin.X + scale, origin.Y + scale, origin.Z);

                break;
            case BlockFace.Bottom:
                v0 = new Vector3D<float>(origin.X, origin.Y, origin.Z + scale);
                v1 = new Vector3D<float>(origin.X, origin.Y, origin.Z);
                v2 = new Vector3D<float>(origin.X + scale, origin.Y, origin.Z);
                v3 = new Vector3D<float>(origin.X + scale, origin.Y, origin.Z + scale);

                break;
            case BlockFace.Front:
                v0 = new Vector3D<float>(origin.X, origin.Y, origin.Z + scale);
                v1 = new Vector3D<float>(origin.X, origin.Y + scale, origin.Z + scale);
                v2 = new Vector3D<float>(origin.X + scale, origin.Y + scale, origin.Z + scale);
                v3 = new Vector3D<float>(origin.X + scale, origin.Y, origin.Z + scale);

                break;
            case BlockFace.Back:
                v0 = new Vector3D<float>(origin.X + scale, origin.Y, origin.Z);
                v1 = new Vector3D<float>(origin.X + scale, origin.Y + scale, origin.Z);
                v2 = new Vector3D<float>(origin.X, origin.Y + scale, origin.Z);
                v3 = new Vector3D<float>(origin.X, origin.Y, origin.Z);

                break;
            case BlockFace.Right:
                v0 = new Vector3D<float>(origin.X + scale, origin.Y, origin.Z + scale);
                v1 = new Vector3D<float>(origin.X + scale, origin.Y + scale, origin.Z + scale);
                v2 = new Vector3D<float>(origin.X + scale, origin.Y + scale, origin.Z);
                v3 = new Vector3D<float>(origin.X + scale, origin.Y, origin.Z);

                break;
            case BlockFace.Left:
                v0 = new Vector3D<float>(origin.X, origin.Y, origin.Z);
                v1 = new Vector3D<float>(origin.X, origin.Y + scale, origin.Z);
                v2 = new Vector3D<float>(origin.X, origin.Y + scale, origin.Z + scale);
                v3 = new Vector3D<float>(origin.X, origin.Y, origin.Z + scale);

                break;
            default:
                return;
        }

        t0 = new Vector2D<float>(0f, 0f);
        t1 = new Vector2D<float>(0f, 1f);
        t2 = new Vector2D<float>(1f, 1f);
        t3 = new Vector2D<float>(1f, 0f);

        var direction = (float)GetDirectionIndex(face);
        var isTop = face == BlockFace.Top ? 1f : 0f;

        vertices.Add(new ChunkFluidVertex(v0, color, t0, tileBase, tileSize, direction, isTop));
        vertices.Add(new ChunkFluidVertex(v1, color, t1, tileBase, tileSize, direction, isTop));
        vertices.Add(new ChunkFluidVertex(v2, color, t2, tileBase, tileSize, direction, isTop));
        vertices.Add(new ChunkFluidVertex(v3, color, t3, tileBase, tileSize, direction, isTop));

        indices.Add(baseIndex);
        indices.Add(baseIndex + 1);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 3);
    }

    /// <summary>
    /// Render info for a single face - used during greedy meshing.
    /// </summary>
    private readonly struct FaceRenderInfo
    {
        public FaceRenderInfo(int x, int y, int z, Vector4D<byte> lighting, BlockType blockType)
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
        public Vector4D<byte> Lighting { get; }
        public BlockType BlockType { get; }
        public bool HasValue { get; }
    }
}
