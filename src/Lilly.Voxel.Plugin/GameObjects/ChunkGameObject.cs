using System;
using Lilly.Engine.Core.Data.Privimitives;
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

    // GPU Resources for each geometry type
    private VertexBuffer<ChunkVertex>? _solidVertexBuffer;
    private VertexBuffer<ChunkVertex>? _billboardVertexBuffer;
    private VertexBuffer<ChunkFluidVertex>? _fluidVertexBuffer;
    private VertexBuffer<ChunkItemVertex>? _itemVertexBuffer;

    private uint _solidIndexCount;
    private uint _billboardIndexCount;
    private uint _fluidIndexCount;
    private uint _itemIndexCount;

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
    /// Uploads mesh data to GPU buffers.
    /// </summary>
    private void UploadMeshData(ChunkMeshData meshData)
    {
        DisposeBuffers();

        // Upload solid geometry
        if (meshData.Vertices.Length > 0 && meshData.Indices.Length > 0)
        {
            _solidVertexBuffer = new VertexBuffer<ChunkVertex>(
                GraphicsDevice,
                meshData.Vertices,
                BufferUsage.StaticCopy
            );
            _solidIndexCount = (uint)meshData.Indices.Length;
        }

        // Upload billboard geometry
        if (meshData.BillboardVertices.Length > 0 && meshData.BillboardIndices.Length > 0)
        {
            _billboardVertexBuffer = new VertexBuffer<ChunkVertex>(
                GraphicsDevice,
                meshData.BillboardVertices,
                BufferUsage.StaticCopy
            );
            _billboardIndexCount = (uint)meshData.BillboardIndices.Length;
        }

        // Upload fluid geometry
        if (meshData.FluidVertices.Length > 0 && meshData.FluidIndices.Length > 0)
        {
            _fluidVertexBuffer = new VertexBuffer<ChunkFluidVertex>(
                GraphicsDevice,
                meshData.FluidVertices,
                BufferUsage.StaticCopy
            );
            _fluidIndexCount = (uint)meshData.FluidIndices.Length;
        }

        // Upload item geometry
        if (meshData.ItemVertices.Length > 0 && meshData.ItemIndices.Length > 0)
        {
            _itemVertexBuffer = new VertexBuffer<ChunkItemVertex>(
                GraphicsDevice,
                meshData.ItemVertices,
                BufferUsage.StaticCopy
            );
            _itemIndexCount = (uint)meshData.ItemIndices.Length;
        }

        _logger.Information(
            "Mesh uploaded - Solid: {SolidCount}, Billboard: {BillboardCount}, Fluid: {FluidCount}, Item: {ItemCount}",
            _solidIndexCount,
            _billboardIndexCount,
            _fluidIndexCount,
            _itemIndexCount
        );
    }

    /// <inheritdoc />
    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (Chunk == null)
        {
            yield break;
        }

        // Solid geometry
        if (_solidIndexCount > 0 && _solidVertexBuffer != null && _blockShader != null)
        {
            SetSolidUniforms();

            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    _blockShader,
                    _solidVertexBuffer,
                    _solidIndexCount,
                    PrimitiveType.Triangles
                )
            );
        }

        // Billboard geometry
        if (_billboardIndexCount > 0 && _billboardVertexBuffer != null && _billboardShader != null)
        {
            SetBillboardUniforms();

            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    _billboardShader,
                    _billboardVertexBuffer,
                    _billboardIndexCount,
                    PrimitiveType.Triangles
                )
            );
        }

        // Fluid geometry
        if (_fluidIndexCount > 0 && _fluidVertexBuffer != null && _fluidShader != null)
        {
            SetFluidUniforms(gameTime);

            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    _fluidShader,
                    _fluidVertexBuffer,
                    _fluidIndexCount,
                    PrimitiveType.Triangles
                )
            );
        }

        // Item geometry
        if (_itemIndexCount > 0 && _itemVertexBuffer != null && _itemBillboardShader != null)
        {
            // Item uniforms are set in the camera-based Draw method
            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    _itemBillboardShader,
                    _itemVertexBuffer,
                    _itemIndexCount,
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
            if (_itemIndexCount > 0 && _itemBillboardShader != null)
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

            shader.Uniforms["uModel"].SetValueVec3(Transform.Position.ToSystem());
            shader.Uniforms["uView"].SetValueMat4(camera.View.ToSystem());
            shader.Uniforms["uProjection"].SetValueMat4(camera.Projection.ToSystem());
            shader.Uniforms["uFogEnabled"].SetValueBool(FogEnabled);
            shader.Uniforms["uFogColor"].SetValueVec3(FogColor.ToSystem());
            shader.Uniforms["uFogStart"].SetValueFloat(FogStart);
            shader.Uniforms["uFogEnd"].SetValueFloat(FogEnd);
            shader.Uniforms["uAmbient"].SetValueVec3(AmbientLight.ToSystem());
        }
    }

    /// <summary>
    /// Sets uniforms specific to solid block rendering.
    /// </summary>
    private void SetSolidUniforms()
    {
        if (_blockShader == null)
            return;

        _blockShader.Uniforms["uLightDirection"].SetValueVec3(Vector3D.Normalize(LightDirection).ToSystem());
        _blockShader.Uniforms["uLightIntensity"].SetValueFloat(LightIntensity);
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

        // Billboards use simplified lighting
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

        _fluidShader.Uniforms["uLightDirection"].SetValueVec3(Vector3D.Normalize(LightDirection).ToSystem());
        _fluidShader.Uniforms["uTime"].SetValueFloat(_animationTime);
        _fluidShader.Uniforms["uWaterTransparency"].SetValueFloat(WaterTransparency);
    }

    /// <summary>
    /// Sets uniforms specific to item billboard rendering.
    /// </summary>
    private void SetItemUniforms(ICamera3D camera)
    {
        if (_itemBillboardShader == null)
            return;

        _itemBillboardShader.Uniforms["uLightDirection"].SetValueVec3(Vector3D.Normalize(LightDirection).ToSystem());
        _itemBillboardShader.Uniforms["uCameraRight"].SetValueVec3(Vector3D.Normalize(camera.Right).ToSystem());
        _itemBillboardShader.Uniforms["uCameraUp"].SetValueVec3(Vector3D.Normalize(camera.Up).ToSystem());
        _itemBillboardShader.Uniforms["uCameraForward"].SetValueVec3(Vector3D.Normalize(camera.Forward).ToSystem());
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

        _solidIndexCount = 0;
        _billboardIndexCount = 0;
        _fluidIndexCount = 0;
        _itemIndexCount = 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeBuffers();
        GC.SuppressFinalize(this);
    }
}
