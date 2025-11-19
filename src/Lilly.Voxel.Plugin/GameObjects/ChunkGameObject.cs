using System;
using System.Numerics;
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
    private VertexBuffer<VertexColor>? _boundaryVertexBuffer;

    private uint _solidVertexCount;
    private uint _billboardVertexCount;
    private uint _fluidVertexCount;
    private uint _itemVertexCount;
    private uint _boundaryVertexCount;

    // Shader programs
    private ShaderProgram? _blockShader;
    private ShaderProgram? _billboardShader;
    private ShaderProgram? _fluidShader;
    private ShaderProgram? _itemBillboardShader;
    private SimpleShaderProgram? _boundaryShader;

    // Cached camera-dependent uniforms
    private CommonUniformData _commonUniforms;
    private ItemCameraUniformData _itemCameraUniforms;
    private bool _hasCachedCameraUniforms;

    public bool ShowChunkBoundaries { get; set; } = true;
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
    /// Binds a chunk instance to the renderer and schedules a mesh rebuild.
    /// </summary>
    public void SetChunk(ChunkEntity chunk)
    {
        Chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        _meshDirty = true;
        _logger.Information("SetChunk called at position {Position}, mesh marked dirty", Transform.Position);
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

    private void CacheCameraUniforms(ICamera3D camera)
    {
        _commonUniforms = new CommonUniformData
        {
            View = camera.View.ToSystem(),
            Projection = camera.Projection.ToSystem(),
            FogEnabled = FogEnabled,
            FogColor = FogColor.ToSystem(),
            FogStart = FogStart,
            FogEnd = FogEnd,
            Ambient = AmbientLight.ToSystem()
        };

        _itemCameraUniforms = new ItemCameraUniformData
        {
            CameraRight = Vector3D.Normalize(camera.Right).ToSystem(),
            CameraUp = Vector3D.Normalize(camera.Up).ToSystem(),
            CameraForward = Vector3D.Normalize(camera.Forward).ToSystem()
        };

        _hasCachedCameraUniforms = true;

        if (ShowChunkBoundaries)
        {
            UpdateBoundaryShader(camera);
        }
    }

    private void UpdateBoundaryShader(ICamera3D camera)
    {
        EnsureBoundaryResources();

        if (_boundaryShader == null)
        {
            return;
        }

        var world = Transform.GetTransformationMatrix().ToSystem();
        _boundaryShader.World = world;
        _boundaryShader.View = camera.View.ToSystem();
        _boundaryShader.Projection = camera.Projection.ToSystem();
    }

    private void EnsureBoundaryResources()
    {
        if (_boundaryShader == null)
        {
            _boundaryShader = SimpleShaderProgram.Create<VertexColor>(GraphicsDevice);
        }

        if (_boundaryVertexBuffer == null)
        {
            var vertices = BuildBoundaryVertices();
            _boundaryVertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, vertices, BufferUsage.StaticCopy);
            _boundaryVertexCount = (uint)vertices.Length;
        }
    }

    private static VertexColor[] BuildBoundaryVertices()
    {
        var color = new Color4b(255, 200, 50, 255);
        float width = ChunkEntity.Size;
        float height = ChunkEntity.Height;

        Vector3[] corners =
        {
            new(0f, 0f, 0f),
            new(width, 0f, 0f),
            new(width, 0f, width),
            new(0f, 0f, width),
            new(0f, height, 0f),
            new(width, height, 0f),
            new(width, height, width),
            new(0f, height, width)
        };

        var vertices = new VertexColor[24];
        int index = 0;

        void AddEdge(int a, int b)
        {
            vertices[index++] = new VertexColor(corners[a], color);
            vertices[index++] = new VertexColor(corners[b], color);
        }

        // Bottom square
        AddEdge(0, 1);
        AddEdge(1, 2);
        AddEdge(2, 3);
        AddEdge(3, 0);

        // Top square
        AddEdge(4, 5);
        AddEdge(5, 6);
        AddEdge(6, 7);
        AddEdge(7, 4);

        // Vertical edges
        AddEdge(0, 4);
        AddEdge(1, 5);
        AddEdge(2, 6);
        AddEdge(3, 7);

        return vertices;
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
        if (Chunk == null || !_hasCachedCameraUniforms)
        {
            yield break;
        }

        var modelTranslation = Transform.Position.ToSystem();
        var commonUniforms = _commonUniforms;
        var assetManager = _assetManager;

        // Solid geometry
        if (_solidVertexBuffer != null && _blockShader != null)
        {
            var shader = _blockShader;
            var lightDirection = Vector3D.Normalize(LightDirection).ToSystem();
            var lightIntensity = LightIntensity;
            var blockAtlas = _blockAtlasName;

            yield return RenderCommandHelpers.SetUniforms(
                shader,
                s =>
                {
                    ApplyCommonUniforms(s, modelTranslation, commonUniforms);
                    ApplySolidUniforms(s, lightDirection, lightIntensity, blockAtlas, assetManager);
                }
            );

            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    shader,
                    _solidVertexBuffer,
                    _solidVertexCount,
                    PrimitiveType.Triangles
                )
            );
        }

        // Billboard geometry
        if (_billboardVertexBuffer != null && _billboardShader != null)
        {
            var shader = _billboardShader;
            var atlasName = _billboardAtlasName;

            yield return RenderCommandHelpers.SetUniforms(
                shader,
                s =>
                {
                    ApplyCommonUniforms(s, modelTranslation, commonUniforms);
                    ApplyBillboardUniforms(s, atlasName, assetManager);
                }
            );

            yield return RenderCommandHelpers.SetCullMode(SetCullModePayload.None());
            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    shader,
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
            var shader = _fluidShader;
            var lightDirection = Vector3D.Normalize(LightDirection).ToSystem();
            var atlasName = _fluidAtlasName;
            var animationTime = _animationTime;
            var waterTransparency = WaterTransparency;

            yield return RenderCommandHelpers.SetUniforms(
                shader,
                s =>
                {
                    ApplyCommonUniforms(s, modelTranslation, commonUniforms);
                    ApplyFluidUniforms(s, lightDirection, animationTime, waterTransparency, atlasName, assetManager);
                }
            );

            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    shader,
                    _fluidVertexBuffer,
                    _fluidVertexCount,
                    PrimitiveType.Triangles
                )
            );
        }

        // Item geometry
        if (_itemVertexBuffer != null && _itemBillboardShader != null)
        {
            var shader = _itemBillboardShader;
            var lightDirection = Vector3D.Normalize(LightDirection).ToSystem();
            var atlasName = _itemAtlasName;
            var cameraUniforms = _itemCameraUniforms;

            yield return RenderCommandHelpers.SetUniforms(
                shader,
                s =>
                {
                    ApplyCommonUniforms(s, modelTranslation, commonUniforms);
                    ApplyItemUniforms(s, lightDirection, cameraUniforms, atlasName, assetManager);
                }
            );

            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    shader,
                    _itemVertexBuffer,
                    _itemVertexCount,
                    PrimitiveType.Triangles
                )
            );
        }

        if (ShowChunkBoundaries && _boundaryVertexBuffer != null && _boundaryShader != null && _boundaryVertexCount > 0)
        {
            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    _boundaryShader,
                    _boundaryVertexBuffer,
                    _boundaryVertexCount,
                    PrimitiveType.Lines
                )
            );
        }
    }

    /// <inheritdoc />
    public override void Draw(ICamera3D camera, GameTime gameTime)
    {
        if (Chunk != null)
        {
            CacheCameraUniforms(camera);
        }

        base.Draw(camera, gameTime);
    }

    // Legacy uniform helper methods replaced by command-buffer-based uniforms.

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

    private static void ApplyCommonUniforms(ShaderProgram shader, Vector3 modelTranslation, CommonUniformData data)
    {
        shader.TrySetUniform("uModel", uniform => uniform.SetValueVec3(modelTranslation));
        shader.TrySetUniform("uView", uniform => uniform.SetValueMat4(data.View));
        shader.TrySetUniform("uProjection", uniform => uniform.SetValueMat4(data.Projection));
        shader.TrySetUniform("uFogEnabled", uniform => uniform.SetValueBool(data.FogEnabled));
        shader.TrySetUniform("uFogColor", uniform => uniform.SetValueVec3(data.FogColor));
        shader.TrySetUniform("uFogStart", uniform => uniform.SetValueFloat(data.FogStart));
        shader.TrySetUniform("uFogEnd", uniform => uniform.SetValueFloat(data.FogEnd));
        shader.TrySetUniform("uAmbient", uniform => uniform.SetValueVec3(data.Ambient));
    }

    private static void ApplySolidUniforms(
        ShaderProgram shader,
        Vector3 lightDirection,
        float lightIntensity,
        string atlasName,
        IAssetManager assetManager
    )
    {
        shader.TrySetUniform("uLightDirection", uniform => uniform.SetValueVec3(lightDirection));
        shader.TrySetUniform("uLightIntensity", uniform => uniform.SetValueFloat(lightIntensity));
        SetTextureUniform(shader, atlasName, assetManager);
        shader.TrySetUniform("uTexMultiplier", uniform => uniform.SetValueFloat(1.0f));
    }

    private static void ApplyBillboardUniforms(
        ShaderProgram shader,
        string atlasName,
        IAssetManager assetManager
    )
    {
        SetTextureUniform(shader, atlasName, assetManager);
        shader.TrySetUniform("uTexMultiplier", uniform => uniform.SetValueFloat(1.0f));
    }

    private static void ApplyFluidUniforms(
        ShaderProgram shader,
        Vector3 lightDirection,
        float animationTime,
        float waterTransparency,
        string atlasName,
        IAssetManager assetManager
    )
    {
        shader.TrySetUniform("uLightDirection", uniform => uniform.SetValueVec3(lightDirection));
        shader.TrySetUniform("uTime", uniform => uniform.SetValueFloat(animationTime));
        shader.TrySetUniform("uWaterTransparency", uniform => uniform.SetValueFloat(waterTransparency));
        SetTextureUniform(shader, atlasName, assetManager);
        shader.TrySetUniform("uTexMultiplier", uniform => uniform.SetValueFloat(1.0f));
    }

    private static void ApplyItemUniforms(
        ShaderProgram shader,
        Vector3 lightDirection,
        ItemCameraUniformData cameraUniforms,
        string atlasName,
        IAssetManager assetManager
    )
    {
        shader.TrySetUniform("uLightDirection", uniform => uniform.SetValueVec3(lightDirection));
        SetTextureUniform(shader, atlasName, assetManager);
        shader.TrySetUniform("uCameraRight", uniform => uniform.SetValueVec3(cameraUniforms.CameraRight));
        shader.TrySetUniform("uCameraUp", uniform => uniform.SetValueVec3(cameraUniforms.CameraUp));
        shader.TrySetUniform("uCameraForward", uniform => uniform.SetValueVec3(cameraUniforms.CameraForward));
        shader.TrySetUniform("uTexMultiplier", uniform => uniform.SetValueFloat(1.0f));
    }

    private static void SetTextureUniform(ShaderProgram shader, string atlasName, IAssetManager assetManager)
    {
        shader.TrySetUniform(
            "uTexture",
            uniform =>
            {
                try
                {
                    var texture = assetManager.GetTexture<Texture2D>(atlasName + "_atlas");
                    uniform.SetValueTexture(texture);
                }
                catch
                {
                    uniform.SetValueTexture(assetManager.GetWhiteTexture<Texture2D>());
                }
            }
        );
    }

    private struct CommonUniformData
    {
        public Matrix4x4 View;
        public Matrix4x4 Projection;
        public bool FogEnabled;
        public Vector3 FogColor;
        public float FogStart;
        public float FogEnd;
        public Vector3 Ambient;
    }

    private struct ItemCameraUniformData
    {
        public Vector3 CameraRight;
        public Vector3 CameraUp;
        public Vector3 CameraForward;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeBuffers();
        _boundaryVertexBuffer?.Dispose();
        _boundaryShader?.Dispose();
        _boundaryVertexBuffer = null;
        _boundaryShader = null;
        _boundaryVertexCount = 0;
        GC.SuppressFinalize(this);
    }
}
