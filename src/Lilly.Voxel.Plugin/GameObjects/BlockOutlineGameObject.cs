using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Primitives;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

/// <summary>
/// Renders a thin wireframe cube around the block currently targeted by the active camera.
/// </summary>
public sealed class BlockOutlineGameObject : Base3dGameObject, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ICamera3dService _cameraService;
    private VertexBuffer<VertexColor>? _vertexBuffer;
    private SimpleShaderProgram? _shaderProgram;
    private bool _hasTarget;

    public Color4b OutlineColor { get; set; } = Color4b.White;

    public Vector3? TargetBlockPosition { get; private set; }

    /// <summary>
    /// Maximum distance to test when raycasting from the camera.
    /// </summary>
    public float RayDistance { get; set; } = 10.0f;

    public BlockOutlineGameObject(
        GraphicsDevice graphicsDevice,
        IGameObjectManager gameObjectManager,
        ICamera3dService cameraService
    ) : base("BlockOutline", gameObjectManager)
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

        // Render lines on top of the target block
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
        // Define the 8 corners of the unit cube (0,0,0) to (1,1,1)
        var p0 = new Vector3(0, 0, 0);
        var p1 = new Vector3(1, 0, 0);
        var p2 = new Vector3(1, 1, 0);
        var p3 = new Vector3(0, 1, 0);
        var p4 = new Vector3(0, 0, 1);
        var p5 = new Vector3(1, 0, 1);
        var p6 = new Vector3(1, 1, 1);
        var p7 = new Vector3(0, 1, 1);

        // Define lines (pairs of vertices)
        var vertices = new VertexColor[]
        {
            // Bottom face
            new(p0, OutlineColor), new(p1, OutlineColor),
            new(p1, OutlineColor), new(p2, OutlineColor),
            new(p2, OutlineColor), new(p3, OutlineColor),
            new(p3, OutlineColor), new(p0, OutlineColor),

            // Top face
            new(p4, OutlineColor), new(p5, OutlineColor),
            new(p5, OutlineColor), new(p6, OutlineColor),
            new(p6, OutlineColor), new(p7, OutlineColor),
            new(p7, OutlineColor), new(p4, OutlineColor),

            // Vertical edges
            new(p0, OutlineColor), new(p4, OutlineColor),
            new(p1, OutlineColor), new(p5, OutlineColor),
            new(p2, OutlineColor), new(p6, OutlineColor),
            new(p3, OutlineColor), new(p7, OutlineColor)
        };

        _vertexBuffer = new VertexBuffer<VertexColor>(_graphicsDevice, vertices, BufferUsage.StaticDraw);
        _shaderProgram = SimpleShaderProgram.Create<VertexColor>(_graphicsDevice);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var camera = _cameraService.ActiveCamera;

        if (Parent is not WorldGameObject world || camera == null)
        {
            _hasTarget = false;
            TargetBlockPosition = null;

            return;
        }

        var ray = new Ray(
            new(camera.Position.X, camera.Position.Y, camera.Position.Z),
            new(camera.Forward.X, camera.Forward.Y, camera.Forward.Z)
        );

        if (world.Raycast(ray, RayDistance, out var blockPos))
        {
            _hasTarget = true;
            TargetBlockPosition = blockPos;

            // Slightly grow and offset the outline to reduce z-fighting with block faces
            const float inset = 0.001f;
            Transform.Scale = new(1f + inset);
            Transform.Position = new Vector3(blockPos.X, blockPos.Y, blockPos.Z) - new Vector3(inset * 0.5f);
        }
        else
        {
            _hasTarget = false;
            TargetBlockPosition = null;
        }
    }
}
