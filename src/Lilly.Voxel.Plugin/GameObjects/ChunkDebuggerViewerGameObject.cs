using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Utils;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

/// <summary>
/// Draws a wireframe cube representing the chunk the camera is currently inside.
/// Useful for visualizing chunk boundaries during debugging.
/// </summary>
public sealed class ChunkDebuggerViewerGameObject : Base3dGameObject, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ICamera3dService _cameraService;
    private VertexBuffer<VertexColor>? _vertexBuffer;
    private SimpleShaderProgram? _shaderProgram;
    private bool _hasTarget;

    public Color4b ChunkColor { get; set; } = Color4b.Cyan;

    public ChunkDebuggerViewerGameObject(
        GraphicsDevice graphicsDevice,
        IGameObjectManager gameObjectManager,
        ICamera3dService cameraService
    ) : base("ChunkDebuggerViewer", gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        _cameraService = cameraService;
        IgnoreFrustumCulling = true;
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _shaderProgram?.Dispose();
    }

    public override void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera)
    {
        if (!_hasTarget || _vertexBuffer == null || _shaderProgram == null)
        {
            return;
        }

        graphicsDevice.ShaderProgram = _shaderProgram;
        _shaderProgram.World = Transform.GetTransformationMatrix();
        _shaderProgram.View = camera.View;
        _shaderProgram.Projection = camera.Projection;

        var oldBlend = graphicsDevice.BlendState;
        var oldDepth = graphicsDevice.DepthState;
        var oldCulling = graphicsDevice.FaceCullingEnabled;
        var oldCullMode = graphicsDevice.CullFaceMode;

        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthState = DepthState.Default;
        graphicsDevice.FaceCullingEnabled = false;

        graphicsDevice.VertexArray = _vertexBuffer;
        graphicsDevice.DrawArrays(PrimitiveType.Lines, 0, _vertexBuffer.Value.StorageLength);

        graphicsDevice.BlendState = oldBlend;
        graphicsDevice.DepthState = oldDepth;
        graphicsDevice.FaceCullingEnabled = oldCulling;
        graphicsDevice.CullFaceMode = oldCullMode;
    }

    public override void Initialize()
    {
        var p0 = new Vector3(0, 0, 0);
        var p1 = new Vector3(1, 0, 0);
        var p2 = new Vector3(1, 1, 0);
        var p3 = new Vector3(0, 1, 0);
        var p4 = new Vector3(0, 0, 1);
        var p5 = new Vector3(1, 0, 1);
        var p6 = new Vector3(1, 1, 1);
        var p7 = new Vector3(0, 1, 1);

        var vertices = new VertexColor[]
        {
            // Bottom face
            new(p0, ChunkColor), new(p1, ChunkColor),
            new(p1, ChunkColor), new(p2, ChunkColor),
            new(p2, ChunkColor), new(p3, ChunkColor),
            new(p3, ChunkColor), new(p0, ChunkColor),

            // Top face
            new(p4, ChunkColor), new(p5, ChunkColor),
            new(p5, ChunkColor), new(p6, ChunkColor),
            new(p6, ChunkColor), new(p7, ChunkColor),
            new(p7, ChunkColor), new(p4, ChunkColor),

            // Vertical edges
            new(p0, ChunkColor), new(p4, ChunkColor),
            new(p1, ChunkColor), new(p5, ChunkColor),
            new(p2, ChunkColor), new(p6, ChunkColor),
            new(p3, ChunkColor), new(p7, ChunkColor)
        };

        _vertexBuffer = new VertexBuffer<VertexColor>(_graphicsDevice, vertices, BufferUsage.StaticDraw);
        _shaderProgram = SimpleShaderProgram.Create<VertexColor>(_graphicsDevice);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var camera = _cameraService.ActiveCamera;

        if (camera == null)
        {
            _hasTarget = false;

            return;
        }

        var chunkCoords = ChunkUtils.GetChunkCoordinates(camera.Position);
        var chunkOrigin = ChunkUtils.ChunkCoordinatesToWorldPosition(
            (int)chunkCoords.X,
            (int)chunkCoords.Y,
            (int)chunkCoords.Z
        );

        const float inset = 0.01f;
        _hasTarget = true;
        Transform.Position = chunkOrigin - new Vector3(inset * 0.5f);
        Transform.Scale = new(ChunkEntity.Size + inset, ChunkEntity.Height + inset, ChunkEntity.Size + inset);
    }
}
