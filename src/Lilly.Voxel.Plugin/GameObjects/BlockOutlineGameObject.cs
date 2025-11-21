using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Primitives;
using Lilly.Engine.Rendering.Core.Types;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

public class BlockOutlineGameObject : BaseGameObject3D
{
    private readonly ICamera3dService _camera3dService;

    // Dependencies
    public VoxelWorldGameObject? VoxelWorld { get; set; }

    private VertexBuffer<VertexColor> _vertexBuffer;
    private SimpleShaderProgram _shaderProgram;
    private bool _hasTarget;
    private const float RayDistance = 10.0f;

    public BlockOutlineGameObject(GraphicsDevice graphicsDevice, ICamera3dService camera3dService)
        : base(graphicsDevice)
    {
        _camera3dService = camera3dService;
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

        var color = Color4b.White;

        // Define lines (pairs of vertices)
        var vertices = new VertexColor[]
        {
            // Bottom face
            new(p0, color), new(p1, color),
            new(p1, color), new(p2, color),
            new(p2, color), new(p3, color),
            new(p3, color), new(p0, color),

            // Top face
            new(p4, color), new(p5, color),
            new(p5, color), new(p6, color),
            new(p6, color), new(p7, color),
            new(p7, color), new(p4, color),

            // Vertical edges
            new(p0, color), new(p4, color),
            new(p1, color), new(p5, color),
            new(p2, color), new(p6, color),
            new(p3, color), new(p7, color)
        };

        _vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, vertices, BufferUsage.StaticDraw);
        _shaderProgram = SimpleShaderProgram.Create<VertexColor>(GraphicsDevice);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (VoxelWorld == null)
        {
            _hasTarget = false;

            return;
        }

        var camera = _camera3dService.ActiveCamera;

        if (camera == null)
        {
            _hasTarget = false;

            return;
        }


        var ray = new Ray(camera.Position, camera.Forward);

        if (VoxelWorld.Raycast(ray, RayDistance, out var blockPos))
        {
            _hasTarget = true;

            // The blockPos is the integer coordinate of the block (floor).
            // Our cube vertices are 0..1, so setting position to blockPos covers the block.
            Transform.Position = new Vector3D<float>(blockPos.X, blockPos.Y, blockPos.Z);

            // Optional: Scale slightly to avoid Z-fighting if needed
            Transform.Scale = new Vector3D<float>(1.001f);
            Transform.Position -= new Vector3D<float>(0.0005f);
        }
        else
        {
            _hasTarget = false;
        }
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (!_hasTarget)
        {
            yield break;
        }

        yield return new RenderCommand(
            RenderCommandType.DrawArray,
            new DrawArrayPayload(
                _shaderProgram,
                _vertexBuffer,
                _vertexBuffer.StorageLength,
                PrimitiveType.Lines
            )
        );
    }

    public override void Draw(ICamera3D camera, GameTime gameTime)
    {
        if (!_hasTarget)
            return;

        _shaderProgram.Projection = camera.Projection.ToSystem();
        _shaderProgram.View = camera.View.ToSystem();
        _shaderProgram.World = Transform.GetTransformationMatrix().ToSystem();

        base.Draw(camera, gameTime);
    }

    // Need to dispose resources
    public void Dispose()
    {
        _shaderProgram?.Dispose();
    }
}
