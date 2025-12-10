using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Physics;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Physics;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Vertexts;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Primitives;
using TrippyGL;

namespace Lilly.Engine.GameObjects.ThreeD;

/// <summary>
/// Simple animated cube that randomizes its vertex colors over time.
/// </summary>
public class SimpleCubeGameObject : Base3dGameObject, IInitializable, IUpdateble, IDisposable, IPhysicsGameObject3d
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IAssetManager _assetManager;

    private VertexBuffer<VertexPositionNormalTex> _vertexBuffer;
    private ShaderProgram? _shaderProgram;
    private VertexPositionNormalTex[] _cubeVertices = [];
    private Texture2D? _texture;

    public float XRotationSpeed { get; set; }
    public float YRotationSpeed { get; set; }
    public float ZRotationSpeed { get; set; }
    public float Mass { get; set; } = 1f;

    private IPhysicsBodyHandle _body;

    public override BoundingBox BoundingBox
    {
        get
        {
            // Cube size is 1x1x1, calculate half extents considering transform scale
            var halfSize = 0.5f;
            var halfWidth = halfSize * Transform.Scale.X;
            var halfHeight = halfSize * Transform.Scale.Y;
            var halfDepth = halfSize * Transform.Scale.Z;

            var min = Transform.Position - new Vector3(halfWidth, halfHeight, halfDepth);
            var max = Transform.Position + new Vector3(halfWidth, halfHeight, halfDepth);

            return new(min, max);
        }
    }

    public SimpleCubeGameObject(
        GraphicsDevice graphicsDevice,
        IRenderPipeline gameObjectManager,
        IAssetManager assetManager
    ) : base("SimpleCube", gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
    }

    public bool IsStatic => false;

    public event Action? PhysicsShapeDirty;

    public Transform3D PhysicsTransform => Transform;

    public PhysicsSyncMode SyncMode => PhysicsSyncMode.FullPose;

    public PhysicsBodyConfig BuildBodyConfig()

        // Cube size is 1x1x1 (from -0.5 to 0.5 in all dimensions)
        => new(
            new BoxShape(Transform.Scale.X, Transform.Scale.Y, Transform.Scale.Z),
            Mass,
            new(Transform.Position, Transform.Rotation)
        );

    public void Dispose()
    {
        _vertexBuffer.Dispose();

        // Do not dispose _shaderProgram here as it is managed by AssetManager
        GC.SuppressFinalize(this);
    }

    public override void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera)
    {
        if (!IsActive || _shaderProgram == null || _texture == null)
        {
            return;
        }

        graphicsDevice.ShaderProgram = _shaderProgram;

        _shaderProgram.Uniforms["World"].SetValueMat4(Transform.GetTransformationMatrix());
        _shaderProgram.Uniforms["View"].SetValueMat4(camera.View);
        _shaderProgram.Uniforms["Projection"].SetValueMat4(camera.Projection);
        _shaderProgram.Uniforms["Texture"].SetValueTexture(_texture);
        _shaderProgram.Uniforms["LightDir"].SetValueVec3(new(-0.4f, -1.0f, -0.2f));
        _shaderProgram.Uniforms["LightColor"].SetValueVec3(Vector3.One);
        _shaderProgram.Uniforms["Ambient"].SetValueVec3(new(0.15f, 0.15f, 0.15f));
        _shaderProgram.Uniforms["Tint"].SetValueVec4(Vector4.One);

        graphicsDevice.VertexArray = _vertexBuffer;
        graphicsDevice.BlendState = BlendState.NonPremultiplied;

        graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, _vertexBuffer.StorageLength);
    }

    public void Initialize()
    {
        _cubeVertices = CreateCubeVertices();
        var randomBoxTexture = Random.Shared.Next(1, 5);
        _texture = _assetManager.GetTexture<Texture2D>($"box{randomBoxTexture}");

        _vertexBuffer = new(_graphicsDevice, _cubeVertices, BufferUsage.DynamicCopy);

        _shaderProgram = _assetManager.GetShaderProgram("model");
    }

    public void OnPhysicsAttached(IPhysicsBodyHandle h)
    {
        _body = h;
    }

    public void OnPhysicsDetached()
    {
        _body = default!;
    }

    public void Update(GameTime gameTime)
        => base.Update(gameTime);

    private static VertexPositionNormalTex[] CreateCubeVertices()
    {
        var frontBottomLeft = new Vector3(-0.5f, -0.5f, 0.5f);
        var frontBottomRight = new Vector3(0.5f, -0.5f, 0.5f);
        var frontTopLeft = new Vector3(-0.5f, 0.5f, 0.5f);
        var frontTopRight = new Vector3(0.5f, 0.5f, 0.5f);

        var backBottomLeft = new Vector3(-0.5f, -0.5f, -0.5f);
        var backBottomRight = new Vector3(0.5f, -0.5f, -0.5f);
        var backTopLeft = new Vector3(-0.5f, 0.5f, -0.5f);
        var backTopRight = new Vector3(0.5f, 0.5f, -0.5f);

        var uvTopLeft = new Vector2(0, 0);
        var uvTopRight = new Vector2(1, 0);
        var uvBottomLeft = new Vector2(0, 1);
        var uvBottomRight = new Vector2(1, 1);

        return
        [
            // Front (+Z)
            new(frontBottomLeft, Vector3.UnitZ, uvBottomLeft),
            new(frontBottomRight, Vector3.UnitZ, uvBottomRight),
            new(frontTopLeft, Vector3.UnitZ, uvTopLeft),

            new(frontBottomRight, Vector3.UnitZ, uvBottomRight),
            new(frontTopRight, Vector3.UnitZ, uvTopRight),
            new(frontTopLeft, Vector3.UnitZ, uvTopLeft),

            // Back (-Z)
            new(backBottomRight, -Vector3.UnitZ, uvBottomLeft),
            new(backBottomLeft, -Vector3.UnitZ, uvBottomRight),
            new(backTopRight, -Vector3.UnitZ, uvTopLeft),

            new(backBottomLeft, -Vector3.UnitZ, uvBottomRight),
            new(backTopLeft, -Vector3.UnitZ, uvTopRight),
            new(backTopRight, -Vector3.UnitZ, uvTopLeft),

            // Right (+X)
            new(frontBottomRight, Vector3.UnitX, uvBottomLeft),
            new(backBottomRight, Vector3.UnitX, uvBottomRight),
            new(frontTopRight, Vector3.UnitX, uvTopLeft),

            new(backBottomRight, Vector3.UnitX, uvBottomRight),
            new(backTopRight, Vector3.UnitX, uvTopRight),
            new(frontTopRight, Vector3.UnitX, uvTopLeft),

            // Top (+Y)
            new(backTopLeft, Vector3.UnitY, uvTopLeft),
            new(frontTopLeft, Vector3.UnitY, uvBottomLeft),
            new(backTopRight, Vector3.UnitY, uvTopRight),

            new(frontTopLeft, Vector3.UnitY, uvBottomLeft),
            new(frontTopRight, Vector3.UnitY, uvBottomRight),
            new(backTopRight, Vector3.UnitY, uvTopRight),

            // Bottom (-Y)
            new(backBottomLeft, -Vector3.UnitY, uvTopLeft),
            new(backBottomRight, -Vector3.UnitY, uvTopRight),
            new(frontBottomLeft, -Vector3.UnitY, uvBottomLeft),

            new(frontBottomLeft, -Vector3.UnitY, uvBottomLeft),
            new(backBottomRight, -Vector3.UnitY, uvTopRight),
            new(frontBottomRight, -Vector3.UnitY, uvBottomRight),

            // Left (-X)
            new(backBottomLeft, -Vector3.UnitX, uvBottomLeft),
            new(frontBottomLeft, -Vector3.UnitX, uvBottomRight),
            new(backTopLeft, -Vector3.UnitX, uvTopLeft),

            new(frontBottomLeft, -Vector3.UnitX, uvBottomRight),
            new(frontTopLeft, -Vector3.UnitX, uvTopRight),
            new(backTopLeft, -Vector3.UnitX, uvTopLeft)
        ];
    }
}
