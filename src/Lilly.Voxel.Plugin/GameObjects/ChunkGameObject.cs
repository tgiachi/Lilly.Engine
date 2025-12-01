using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Vertexs;
using System.Numerics;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

/// <summary>
/// Renders a single chunk using prebuilt mesh data. Buffers are reused until new mesh data arrives.
/// </summary>
public sealed class ChunkGameObject : Base3dGameObject
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IAssetManager _assetManager;
    private readonly ShaderProgram _solidShader;
    private readonly ShaderProgram _billboardShader;
    private readonly ShaderProgram _itemShader;
    private readonly ShaderProgram _fluidShader;

    private readonly Vector3 _defaultAmbient = new(0.8f, 0.8f, 0.8f);
    private readonly Vector3 _defaultLightDir = Vector3.Normalize(new Vector3(0.3f, 1f, 0.3f));

    private string _solidAtlasName = string.Empty;
    private string _billboardAtlasName = string.Empty;
    private string _itemAtlasName = string.Empty;
    private string _fluidAtlasName = string.Empty;

    private VertexBuffer<ChunkVertex>? _solidVbo;
    private VertexBuffer<ChunkVertex>? _billboardVbo;
    private VertexBuffer<ChunkItemVertex>? _itemVbo;
    private VertexBuffer<ChunkFluidVertex>? _fluidVbo;

    private ChunkMeshData? _pendingMesh;

    public ChunkEntity Chunk { get; }

    public ChunkGameObject(
        ChunkEntity chunk,
        GraphicsDevice graphicsDevice,
        IAssetManager assetManager,
        IGameObjectManager gameObjectManager
    ) : base($"Chunk_{chunk.ChunkCoordinates}", gameObjectManager)
    {
        Chunk = chunk;
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
        _solidShader = assetManager.GetShaderProgram("chunk_block");
        _billboardShader = assetManager.GetShaderProgram("chunk_billboard");
        _itemShader = assetManager.GetShaderProgram("chunk_item_billboard");
        _fluidShader = assetManager.GetShaderProgram("chunk_fluid");

        Transform.Position = chunk.Position;
        IgnoreFrustumCulling = false;
    }

    public void SetPendingMesh(ChunkMeshData meshData)
    {
        _pendingMesh = meshData;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_pendingMesh != null)
        {
            ApplyMesh(_pendingMesh);
            _pendingMesh = null;
            Chunk.IsMeshDirty = false;
        }
    }

    private void ApplyMesh(ChunkMeshData mesh)
    {
        _solidAtlasName = mesh.SolidAtlasName;
        _billboardAtlasName = mesh.BillboardAtlasName;
        _itemAtlasName = mesh.ItemAtlasName;
        _fluidAtlasName = mesh.FluidAtlasName;
        UploadSolid(mesh);
        UploadBillboards(mesh);
        UploadItems(mesh);
        UploadFluids(mesh);
    }

    private void UploadSolid(ChunkMeshData mesh)
    {
        if (mesh.Vertices.Length == 0 || mesh.Indices.Length == 0)
        {
            _solidVbo?.Dispose();
            _solidVbo = null;
            return;
        }

        var vertexData = Expand(mesh.Vertices, mesh.Indices);

        _solidVbo?.Dispose();
        _solidVbo = new VertexBuffer<ChunkVertex>(_graphicsDevice, vertexData, BufferUsage.StaticDraw);
    }

    private void UploadBillboards(ChunkMeshData mesh)
    {
        if (mesh.BillboardVertices.Length == 0 || mesh.BillboardIndices.Length == 0)
        {
            _billboardVbo?.Dispose();
            _billboardVbo = null;
            return;
        }

        var data = Expand(mesh.BillboardVertices, mesh.BillboardIndices);
        _billboardVbo?.Dispose();
        _billboardVbo = new VertexBuffer<ChunkVertex>(_graphicsDevice, data, BufferUsage.StaticDraw);
    }

    private void UploadItems(ChunkMeshData mesh)
    {
        if (mesh.ItemVertices.Length == 0 || mesh.ItemIndices.Length == 0)
        {
            _itemVbo?.Dispose();
            _itemVbo = null;
            return;
        }

        var data = Expand(mesh.ItemVertices, mesh.ItemIndices);
        _itemVbo?.Dispose();
        _itemVbo = new VertexBuffer<ChunkItemVertex>(_graphicsDevice, data, BufferUsage.StaticDraw);
    }

    private void UploadFluids(ChunkMeshData mesh)
    {
        if (mesh.FluidVertices.Length == 0 || mesh.FluidIndices.Length == 0)
        {
            _fluidVbo?.Dispose();
            _fluidVbo = null;
            return;
        }

        var data = Expand(mesh.FluidVertices, mesh.FluidIndices);
        _fluidVbo?.Dispose();
        _fluidVbo = new VertexBuffer<ChunkFluidVertex>(_graphicsDevice, data, BufferUsage.StaticDraw);
    }

    private static T[] Expand<T>(T[] vertices, int[] indices) where T : unmanaged, IVertex
    {
        if (indices.Length == 0)
        {
            return vertices;
        }

        var expanded = new T[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            expanded[i] = vertices[indices[i]];
        }
        return expanded;
    }

    public override void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera)
    {
        if (!IsActive)
        {
            return;
        }

        if (_solidVbo != null)
        {
            DrawSolid(camera);
        }

        if (_billboardVbo != null)
        {
            DrawBillboards(camera);
        }

        if (_itemVbo != null)
        {
            DrawItems(camera);
        }

        if (_fluidVbo != null)
        {
            DrawFluids(camera);
        }
    }

    private void DrawSolid(ICamera3D camera)
    {
        _graphicsDevice.ShaderProgram = _solidShader;
        _solidShader.Uniforms["uModel"].SetValueVec3(Transform.Position);
        _solidShader.Uniforms["uView"].SetValueMat4(camera.View);
        _solidShader.Uniforms["uProjection"].SetValueMat4(camera.Projection);
        _solidShader.Uniforms["uTexture"].SetValueTexture(GetAtlasTexture(_solidAtlasName));
        _solidShader.Uniforms["uTexMultiplier"].SetValueFloat(1.0f);
        _solidShader.Uniforms["uAmbient"].SetValueVec3(_defaultAmbient);
        _solidShader.Uniforms["uLightDirection"].SetValueVec3(_defaultLightDir);
        _solidShader.Uniforms["uFogEnabled"].SetValueBool(false);

        _graphicsDevice.VertexArray = _solidVbo;
        _graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, _solidVbo!.Value.StorageLength);
    }

    private void DrawBillboards(ICamera3D camera)
    {
        _graphicsDevice.ShaderProgram = _billboardShader;
        _billboardShader.Uniforms["uModel"].SetValueVec3(Transform.Position);
        _billboardShader.Uniforms["uView"].SetValueMat4(camera.View);
        _billboardShader.Uniforms["uProjection"].SetValueMat4(camera.Projection);
        _billboardShader.Uniforms["uTexture"].SetValueTexture(GetAtlasTexture(_billboardAtlasName));
        _billboardShader.Uniforms["uTexMultiplier"].SetValueFloat(1.0f);
        _billboardShader.Uniforms["uAmbient"].SetValueVec3(_defaultAmbient);
        _billboardShader.Uniforms["uFogEnabled"].SetValueBool(false);

        _graphicsDevice.VertexArray = _billboardVbo;
        _graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, _billboardVbo!.Value.StorageLength);
    }

    private void DrawItems(ICamera3D camera)
    {
        _graphicsDevice.ShaderProgram = _itemShader;
        _itemShader.Uniforms["uModel"].SetValueVec3(Transform.Position);
        _itemShader.Uniforms["uView"].SetValueMat4(camera.View);
        _itemShader.Uniforms["uProjection"].SetValueMat4(camera.Projection);
        _itemShader.Uniforms["uTexMultiplier"].SetValueFloat(1.0f);
        _itemShader.Uniforms["uTexture"].SetValueTexture(GetAtlasTexture(_itemAtlasName));
        _itemShader.Uniforms["uAmbient"].SetValueVec3(_defaultAmbient);
        _itemShader.Uniforms["uLightDirection"].SetValueVec3(_defaultLightDir);
        _itemShader.Uniforms["uFogEnabled"].SetValueBool(false);

        _graphicsDevice.VertexArray = _itemVbo;
        _graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, _itemVbo!.Value.StorageLength);
    }

    private void DrawFluids(ICamera3D camera)
    {
        _graphicsDevice.ShaderProgram = _fluidShader;
        _fluidShader.Uniforms["uModel"].SetValueVec3(Transform.Position);
        _fluidShader.Uniforms["uView"].SetValueMat4(camera.View);
        _fluidShader.Uniforms["uProjection"].SetValueMat4(camera.Projection);
        _fluidShader.Uniforms["uTexMultiplier"].SetValueFloat(1.0f);
        _fluidShader.Uniforms["uTexture"].SetValueTexture(GetAtlasTexture(_fluidAtlasName));
        _fluidShader.Uniforms["uWaterTransparency"].SetValueFloat(0.2f);
        _fluidShader.Uniforms["uAmbient"].SetValueVec3(_defaultAmbient);
        _fluidShader.Uniforms["uLightDirection"].SetValueVec3(_defaultLightDir);
        _fluidShader.Uniforms["uFogEnabled"].SetValueBool(false);

        _graphicsDevice.VertexArray = _fluidVbo;
        _graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, _fluidVbo!.Value.StorageLength);
    }

    public void DisposeBuffers()
    {
        _solidVbo?.Dispose();
        _billboardVbo?.Dispose();
        _itemVbo?.Dispose();
        _fluidVbo?.Dispose();
    }

    private Texture2D GetAtlasTexture(string atlasName)
    {
        if (!string.IsNullOrEmpty(atlasName))
        {
            var name = atlasName + "_atlas";
            try
            {
                return _assetManager.GetTexture<Texture2D>(name);
            }
            catch
            {
                // ignore and fallback
            }
        }

        return _assetManager.GetWhiteTexture<Texture2D>();
    }
}
