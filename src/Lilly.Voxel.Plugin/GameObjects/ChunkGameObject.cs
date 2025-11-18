using System;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Extensions;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Helpers;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Types;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Primitives.Vertex;
using Lilly.Voxel.Plugin.Services;
using Lilly.Voxel.Plugin.Types;
using Serilog;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

/// <summary>
/// Renders a single voxel chunk with optimized greedy meshed geometry.
/// Supports solid blocks, billboards, items, and fluid rendering.
/// </summary>
public sealed class ChunkGameObject : BaseGameObject3D, IDisposable
{
    private readonly ChunkMeshBuilder _meshBuilder;
    private readonly IAssetManager _assetManager;
    private readonly IBlockRegistry _blockRegistry;
    private readonly ILogger _logger = Log.ForContext<ChunkGameObject>();

    private ChunkMeshData? _cachedMeshData;
    private bool _meshDirty = true;
    private float _animationTime;
    private string _blockAtlasName = "blocks"; // Default atlas names
    private string _billboardAtlasName = "blocks";
    private string _fluidAtlasName = "blocks";
    private string _itemAtlasName = "blocks";

    // GPU Resources for each geometry type
    private VertexBuffer<ChunkVertex>? _solidVertexBuffer;
    private VertexBuffer<ChunkVertex>? _billboardVertexBuffer;
    private VertexBuffer<ChunkFluidVertex>? _fluidVertexBuffer;
    private VertexBuffer<ChunkItemVertex>? _itemVertexBuffer;

    private uint _solidVertexCount;
    private uint _billboardVertexCount;
    private uint _fluidVertexCount;
    private uint _itemVertexCount;

    // Shader programs
    private ShaderProgram? _blockShader;
    private ShaderProgram? _billboardShader;
    private ShaderProgram? _fluidShader;
    private ShaderProgram? _itemBillboardShader;

    // Rendering configuration
    public bool WireframeEnabled { get; set; }
    public bool ShowChunkBoundaries { get; set; }
    public bool FogEnabled { get; set; } = true;
    public Vector3D<float> FogColor { get; set; } = new(0.7f, 0.8f, 0.9f);
    public float FogStart { get; set; } = 48f;
    public float FogEnd { get; set; } = 160f;
    public Vector3D<float> AmbientLight { get; set; } = new(0.5f, 0.5f, 0.5f);
    public Vector3D<float> LightDirection { get; set; } = new(0.8f, 1.0f, 0.7f);
    public float LightIntensity { get; set; } = 1.5f;
    public float WaterTransparency { get; set; } = 0.3f;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkGameObject"/> class.
    /// </summary>
    public ChunkGameObject(
        GraphicsDevice graphicsDevice,
        ChunkMeshBuilder meshBuilder,
        IAssetManager assetManager,
        IBlockRegistry blockRegistry
    ) : base(graphicsDevice)
    {
        _meshBuilder = meshBuilder;
        _assetManager = assetManager;
        _blockRegistry = blockRegistry;

        IgnoreFrustumCulling = true;
    }

    /// <summary>
    /// Gets the chunk currently bound to this renderer.
    /// </summary>
    public ChunkEntity? Chunk { get; private set; }

    /// <summary>
    /// Gets the coordinates of the chunk within the world grid.
    /// </summary>
    public ChunkCoordinates Coordinates { get; set; }

    /// <summary>
    /// Binds a chunk instance to the renderer and schedules a mesh rebuild.
    /// </summary>
    public void SetChunk(ChunkEntity chunk)
    {
        Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        _meshDirty = true;
    }

    /// <summary>
    /// Marks the current geometry as invalid so it will be rebuilt on the next update.
    /// </summary>
    public void InvalidateGeometry()
    {
        _meshDirty = true;
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        LoadShaders();
        base.Initialize();
    }

    /// <summary>
    /// Loads shader programs for chunk rendering.
    /// </summary>
    private void LoadShaders()
    {
        try
        {
            _blockShader = _assetManager.GetShaderProgram("chunk_block");
            _billboardShader = _assetManager.GetShaderProgram("chunk_billboard");
            _fluidShader = _assetManager.GetShaderProgram("chunk_fluid");
            _itemBillboardShader = _assetManager.GetShaderProgram("chunk_item_billboard");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load chunk shaders");
        }
    }

    /// <inheritdoc />
    public override void Update(GameTime gameTime)
    {
        var delta = gameTime.GetElapsedSeconds();
        _animationTime += delta;

        base.Update(gameTime);

        if (Chunk == null)
        {
            return;
        }

        // Rebuild mesh if dirty
        if (_meshDirty)
        {
            BuildAndUploadMesh();
        }
    }

    /// <summary>
    /// Builds mesh data and uploads it to GPU buffers.
    /// </summary>
    private void BuildAndUploadMesh()
    {
        if (Chunk == null)
        {
            return;
        }

        try
        {
            // Extract atlas names used by different render types
            ExtractAtlasNames();

            // Build mesh data from chunk
            _cachedMeshData = _meshBuilder.BuildMeshData(Chunk, null);

            // Upload to GPU
            UploadMeshData(_cachedMeshData);

            _meshDirty = false;
            _logger.Information("Mesh built and uploaded for chunk at {Position}", Transform.Position);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to build chunk mesh");
        }
    }

    /// <summary>
    /// Extracts atlas names used by the various render types present in the chunk.
    /// </summary>
    private void ExtractAtlasNames()
    {
        if (Chunk == null)
            return;

        _blockAtlasName = "blocks";
        _billboardAtlasName = "blocks";
        _fluidAtlasName = "blocks";
        _itemAtlasName = "blocks";

        // Count non-air blocks for debugging
        int nonAirBlockCount = 0;
        bool solidAtlasSet = false;
        bool billboardAtlasSet = false;
        bool fluidAtlasSet = false;
        bool itemAtlasSet = false;

        for (int i = 0; i < Chunk.Blocks.Length; i++)
        {
            var blockId = Chunk.Blocks[i];

            if (blockId != 0) // Not air
            {
                nonAirBlockCount++;
                var block = _blockRegistry.GetById(blockId);

                if (!solidAtlasSet &&
                    block.RenderType is BlockRenderType.Solid or BlockRenderType.Transparent or BlockRenderType.Cutout)
                {
                    var texture = block.TextureSet.Top;
                    _blockAtlasName = texture.AtlasName;
                    solidAtlasSet = true;
                }

                if (!billboardAtlasSet && block.RenderType == BlockRenderType.Billboard)
                {
                    var texture = block.TextureSet.GetTextureForFace(BlockFace.Front);
                    _billboardAtlasName = texture.AtlasName;
                    billboardAtlasSet = true;
                }

                if (!fluidAtlasSet && block.RenderType == BlockRenderType.Fluid)
                {
                    var texture = block.TextureSet.Top;
                    _fluidAtlasName = texture.AtlasName;
                    fluidAtlasSet = true;
                }

                if (!itemAtlasSet && block.RenderType == BlockRenderType.Item)
                {
                    var texture = block.TextureSet.GetTextureForFace(BlockFace.Front);
                    _itemAtlasName = texture.AtlasName;
                    itemAtlasSet = true;
                }

                if (solidAtlasSet && billboardAtlasSet && fluidAtlasSet && itemAtlasSet)
                {
                    break;
                }
            }
        }

        _logger.Information(
            "Chunk analysis - Total blocks: {Total}, Non-air blocks: {NonAir}, Percentage: {Percentage:P}",
            Chunk.Blocks.Length,
            nonAirBlockCount,
            nonAirBlockCount / (float)Chunk.Blocks.Length
        );

        if (nonAirBlockCount == 0)
        {
            _logger.Warning("Chunk is completely empty (all air)");
        }
    }

    /// <summary>
    /// Uploads mesh data to GPU buffers.
    /// </summary>
    private void UploadMeshData(ChunkMeshData meshData)
    {
        DisposeBuffers();

        var expandedSolidVertices = ExpandIndexedGeometry(meshData.Vertices, meshData.Indices);
        var expandedBillboardVertices = ExpandIndexedGeometry(meshData.BillboardVertices, meshData.BillboardIndices);
        var expandedFluidVertices = ExpandIndexedGeometry(meshData.FluidVertices, meshData.FluidIndices);
        var expandedItemVertices = ExpandIndexedGeometry(meshData.ItemVertices, meshData.ItemIndices);

        _logger.Debug(
            "Uploading mesh data - Solid: {SolidVerts}/{SolidIndices}, Billboard: {BbVerts}/{BbIndices}, Fluid: {FluidVerts}/{FluidIndices}, Item: {ItemVerts}/{ItemIndices}",
            expandedSolidVertices.Length,
            meshData.Indices.Length,
            expandedBillboardVertices.Length,
            meshData.BillboardIndices.Length,
            expandedFluidVertices.Length,
            meshData.FluidIndices.Length,
            expandedItemVertices.Length,
            meshData.ItemIndices.Length
        );

        // Upload solid geometry
        if (expandedSolidVertices.Length > 0)
        {
            _solidVertexBuffer = new VertexBuffer<ChunkVertex>(
                GraphicsDevice,
                expandedSolidVertices,
                BufferUsage.StaticCopy
            );
            _solidVertexCount = (uint)expandedSolidVertices.Length;
        }

        // Upload billboard geometry
        if (expandedBillboardVertices.Length > 0)
        {
            _billboardVertexBuffer = new VertexBuffer<ChunkVertex>(
                GraphicsDevice,
                expandedBillboardVertices,
                BufferUsage.StaticCopy
            );
            _billboardVertexCount = (uint)expandedBillboardVertices.Length;
        }

        // Upload fluid geometry
        if (expandedFluidVertices.Length > 0)
        {
            _fluidVertexBuffer = new VertexBuffer<ChunkFluidVertex>(
                GraphicsDevice,
                expandedFluidVertices,
                BufferUsage.StaticCopy
            );
            _fluidVertexCount = (uint)expandedFluidVertices.Length;
        }

        // Upload item geometry
        if (expandedItemVertices.Length > 0)
        {
            _itemVertexBuffer = new VertexBuffer<ChunkItemVertex>(
                GraphicsDevice,
                expandedItemVertices,
                BufferUsage.StaticCopy
            );
            _itemVertexCount = (uint)expandedItemVertices.Length;
        }

        _logger.Information(
            "Mesh uploaded to GPU - Solid: {SolidVertices} vertices, Billboard: {BillboardVertices} vertices, Fluid: {FluidVertices} vertices, Item: {ItemVertices} vertices",
            _solidVertexCount,
            _billboardVertexCount,
            _fluidVertexCount,
            _itemVertexCount
        );
    }

    private static T[] ExpandIndexedGeometry<T>(T[] vertices, int[] indices) where T : struct
    {
        if (vertices.Length == 0 || indices.Length == 0)
        {
            return Array.Empty<T>();
        }

        var expanded = new T[indices.Length];

        for (int i = 0; i < indices.Length; i++)
        {
            int index = indices[i];

            if ((uint)index >= (uint)vertices.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(indices),
                    $"Index {index} is out of range for vertex array length {vertices.Length}"
                );
            }

            expanded[i] = vertices[index];
        }

        return expanded;
    }

    /// <inheritdoc />
    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (Chunk == null)
        {
            yield break;
        }

        // Solid geometry
        if (_solidVertexBuffer != null && _blockShader != null)
        {
            SetSolidUniforms();

            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    _blockShader,
                    _solidVertexBuffer,
                    _solidVertexCount,
                    PrimitiveType.Triangles
                )
            );
        }

        // Billboard geometry
        if (_billboardVertexBuffer != null && _billboardShader != null)
        {
            SetBillboardUniforms();

            yield return RenderCommandHelpers.SetCullMode(SetCullModePayload.None());
            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    _billboardShader,
                    _billboardVertexBuffer,
                    _billboardVertexCount,
                    PrimitiveType.Triangles
                )
            );
            yield return RenderCommandHelpers.SetCullMode(SetCullModePayload.Back());
        }

        // Fluid geometry
        if (_fluidVertexBuffer != null && _fluidShader != null)
        {
            SetFluidUniforms(gameTime);

            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    _fluidShader,
                    _fluidVertexBuffer,
                    _fluidVertexCount,
                    PrimitiveType.Triangles
                )
            );
        }

        // Item geometry
        if (_itemVertexBuffer != null && _itemBillboardShader != null)
        {
            // Item uniforms are set in the camera-based Draw method
            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    _itemBillboardShader,
                    _itemVertexBuffer,
                    _itemVertexCount,
                    PrimitiveType.Triangles
                )
            );
        }
    }

    /// <inheritdoc />
    public override void Draw(ICamera3D camera, GameTime gameTime)
    {
        if (Chunk != null)
        {
            // Set common uniforms that require camera
            SetCommonUniforms(camera);

            // Set item-specific uniforms that require camera
            if (_itemBillboardShader != null)
            {
                SetItemUniforms(camera);
            }
        }

        base.Draw(camera, gameTime);
    }

    /// <summary>
    /// Sets common uniforms for all shaders.
    /// </summary>
    private void SetCommonUniforms(ICamera3D camera)
    {
        var shaders = new[] { _blockShader, _billboardShader, _fluidShader, _itemBillboardShader };

        foreach (var shader in shaders)
        {
            if (shader == null)
            {
                continue;
            }

            shader.TrySetUniform("uModel", uniform => uniform.SetValueVec3(Transform.Position.ToSystem()));
            shader.TrySetUniform("uView", uniform => uniform.SetValueMat4(camera.View.ToSystem()));
            shader.TrySetUniform("uProjection", uniform => uniform.SetValueMat4(camera.Projection.ToSystem()));
            shader.TrySetUniform("uFogEnabled", uniform => uniform.SetValueBool(FogEnabled));
            shader.TrySetUniform("uFogColor", uniform => uniform.SetValueVec3(FogColor.ToSystem()));
            shader.TrySetUniform("uFogStart", uniform => uniform.SetValueFloat(FogStart));
            shader.TrySetUniform("uFogEnd", uniform => uniform.SetValueFloat(FogEnd));
            shader.TrySetUniform("uAmbient", uniform => uniform.SetValueVec3(AmbientLight.ToSystem()));
        }
    }

    /// <summary>
    /// Sets uniforms specific to solid block rendering.
    /// </summary>
    private void SetSolidUniforms()
    {
        if (_blockShader == null)
            return;

        _blockShader.TrySetUniform(
            "uLightDirection",
            uniform => uniform.SetValueVec3(Vector3D.Normalize(LightDirection).ToSystem())
        );
        _blockShader.TrySetUniform("uLightIntensity", uniform => uniform.SetValueFloat(LightIntensity));

        // Set the texture atlas for block rendering
        _blockShader.TrySetUniform(
            "uTexture",
            uniform =>
            {
                try
                {
                    var textureName = _blockAtlasName + "_atlas";
                    var texture = _assetManager.GetTexture<Texture2D>(textureName);
                    uniform.SetValueTexture(texture);
                }
                catch
                {
                    // Fallback to white texture if atlas is not available
                    uniform.SetValueTexture(_assetManager.GetWhiteTexture<Texture2D>());
                }
            }
        );

        // Set texture multiplier (typically 1.0 for standard texture atlases)
        _blockShader.TrySetUniform("uTexMultiplier", uniform => uniform.SetValueFloat(1.0f));
    }

    /// <summary>
    /// Sets uniforms specific to billboard rendering.
    /// </summary>
    private void SetBillboardUniforms()
    {
        if (_billboardShader == null)
        {
            return;
        }

        // Set the texture atlas for billboard rendering
        _billboardShader.TrySetUniform(
            "uTexture",
            uniform =>
            {
                try
                {
                    var textureName = _billboardAtlasName + "_atlas";
                    var texture = _assetManager.GetTexture<Texture2D>(textureName);
                    uniform.SetValueTexture(texture);
                }
                catch
                {
                    uniform.SetValueTexture(_assetManager.GetWhiteTexture<Texture2D>());
                }
            }
        );

        _billboardShader.TrySetUniform("uTexMultiplier", uniform => uniform.SetValueFloat(1.0f));
    }

    /// <summary>
    /// Sets uniforms specific to fluid rendering.
    /// </summary>
    private void SetFluidUniforms(GameTime gameTime)
    {
        if (_fluidShader == null)
        {
            return;
        }

        _fluidShader.TrySetUniform(
            "uLightDirection",
            uniform => uniform.SetValueVec3(Vector3D.Normalize(LightDirection).ToSystem())
        );
        _fluidShader.TrySetUniform("uTime", uniform => uniform.SetValueFloat(_animationTime));
        _fluidShader.TrySetUniform("uWaterTransparency", uniform => uniform.SetValueFloat(WaterTransparency));

        // Set the texture atlas for fluid rendering
        _fluidShader.TrySetUniform(
            "uTexture",
            uniform =>
            {
                try
                {
                    var textureName = _fluidAtlasName + "_atlas";
                    var texture = _assetManager.GetTexture<Texture2D>(textureName);
                    uniform.SetValueTexture(texture);
                }
                catch
                {
                    uniform.SetValueTexture(_assetManager.GetWhiteTexture<Texture2D>());
                }
            }
        );

        _fluidShader.TrySetUniform("uTexMultiplier", uniform => uniform.SetValueFloat(1.0f));
    }

    /// <summary>
    /// Sets uniforms specific to item billboard rendering.
    /// </summary>
    private void SetItemUniforms(ICamera3D camera)
    {
        if (_itemBillboardShader == null)
            return;

        _itemBillboardShader.TrySetUniform(
            "uLightDirection",
            uniform => uniform.SetValueVec3(Vector3D.Normalize(LightDirection).ToSystem())
        );
        _itemBillboardShader.TrySetUniform(
            "uTexture",
            uniform =>
            {
                try
                {
                    var textureName = _itemAtlasName + "_atlas";
                    var texture = _assetManager.GetTexture<Texture2D>(textureName);
                    uniform.SetValueTexture(texture);
                }
                catch
                {
                    uniform.SetValueTexture(_assetManager.GetWhiteTexture<Texture2D>());
                }
            }
        );
        _itemBillboardShader.TrySetUniform(
            "uCameraRight",
            uniform => uniform.SetValueVec3(Vector3D.Normalize(camera.Right).ToSystem())
        );
        _itemBillboardShader.TrySetUniform(
            "uCameraUp",
            uniform => uniform.SetValueVec3(Vector3D.Normalize(camera.Up).ToSystem())
        );
        _itemBillboardShader.TrySetUniform(
            "uCameraForward",
            uniform => uniform.SetValueVec3(Vector3D.Normalize(camera.Forward).ToSystem())
        );
        _itemBillboardShader.TrySetUniform(
            "uTexMultiplier",
            uniform => uniform.SetValueFloat(1.0f)
        );
    }

    /// <summary>
    /// Releases GPU resources.
    /// </summary>
    private void DisposeBuffers()
    {
        _solidVertexBuffer?.Dispose();
        _billboardVertexBuffer?.Dispose();
        _fluidVertexBuffer?.Dispose();
        _itemVertexBuffer?.Dispose();

        _solidVertexBuffer = null;
        _billboardVertexBuffer = null;
        _fluidVertexBuffer = null;
        _itemVertexBuffer = null;

        _solidVertexCount = 0;
        _billboardVertexCount = 0;
        _fluidVertexCount = 0;
        _itemVertexCount = 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeBuffers();
        GC.SuppressFinalize(this);
    }
}
