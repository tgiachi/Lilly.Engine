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
/// Simple box object with customizable width, height, and depth dimensions.
/// </summary>
public class SimpleBoxGameObject : Base3dShadowGameObject, IInitializable, IUpdateble, IDisposable, IPhysicsGameObject3d
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IAssetManager _assetManager;

    private VertexBuffer<VertexPositionNormalTex> _vertexBuffer;
    private ShaderProgram? _shaderProgram;
    private VertexPositionNormalTex[] _boxVertices = [];
    private Texture2D? _texture;
    private bool _needsRebuild;

    private float _width = 1f;
    private float _height = 1f;
    private float _depth = 1f;

    public float Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                _needsRebuild = true;
            }
        }
    }

    public float Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                _needsRebuild = true;
            }
        }
    }

    public float Depth
    {
        get => _depth;
        set
        {
            if (_depth != value)
            {
                _depth = value;
                _needsRebuild = true;
            }
        }
    }

    public string TextureName { get; set; } = "box1";
    public Color4b BoxColor { get; set; } = Color4b.White;

    public override BoundingBox BoundingBox
    {
        get
        {
            // Calculate half extents considering transform scale
            var halfWidth = Width * Transform.Scale.X / 2f;
            var halfHeight = Height * Transform.Scale.Y / 2f;
            var halfDepth = Depth * Transform.Scale.Z / 2f;

            var min = Transform.Position - new Vector3(halfWidth, halfHeight, halfDepth);
            var max = Transform.Position + new Vector3(halfWidth, halfHeight, halfDepth);

            return new(min, max);
        }
    }

    public SimpleBoxGameObject(
        GraphicsDevice graphicsDevice,
        IRenderPipeline gameObjectManager,
        IAssetManager assetManager,
        float width = 1f,
        float height = 1f,
        float depth = 1f
    ) : base("SimpleBox", gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
        Width = width;
        Height = height;
        Depth = depth;
    }

    public bool IsStatic => true;

    public event Action? PhysicsShapeDirty;

    public Transform3D PhysicsTransform => Transform;

    public PhysicsSyncMode SyncMode => PhysicsSyncMode.FullPose;

    public PhysicsBodyConfig BuildBodyConfig()
        => new(
            new BoxShape(Width * Transform.Scale.X, Height * Transform.Scale.Y, Depth * Transform.Scale.Z),
            0f,
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

        if (_needsRebuild)
        {
            RebuildGeometry();
        }

        graphicsDevice.ShaderProgram = _shaderProgram;

        _shaderProgram.Uniforms["World"].SetValueMat4(Transform.GetTransformationMatrix());
        _shaderProgram.Uniforms["View"].SetValueMat4(camera.View);
        _shaderProgram.Uniforms["Projection"].SetValueMat4(camera.Projection);
        _shaderProgram.Uniforms["Texture"].SetValueTexture(_texture);
        _shaderProgram.Uniforms["LightDir"].SetValueVec3(new(-0.4f, -1.0f, -0.2f));
        _shaderProgram.Uniforms["LightColor"].SetValueVec3(Vector3.One);
        _shaderProgram.Uniforms["Ambient"].SetValueVec3(new(0.15f, 0.15f, 0.15f));
        _shaderProgram.Uniforms["Tint"].SetValueVec4(ToVector4(BoxColor));

        graphicsDevice.VertexArray = _vertexBuffer;
        graphicsDevice.BlendState = BlendState.NonPremultiplied;

        graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, _vertexBuffer.StorageLength);
    }

    public void Initialize()
    {
        _boxVertices = CreateBoxVertices();
        _texture = _assetManager.GetTexture<Texture2D>(TextureName);

        _vertexBuffer = new(_graphicsDevice, _boxVertices, BufferUsage.DynamicCopy);

        _shaderProgram = _assetManager.GetShaderProgram("model");
    }

    public void OnPhysicsAttached(IPhysicsBodyHandle h) { }

    public void OnPhysicsDetached() { }

    public void Update(GameTime gameTime)
        => base.Update(gameTime);

    private VertexPositionNormalTex[] CreateBoxVertices()
    {
        var halfWidth = Width / 2f;
        var halfHeight = Height / 2f;
        var halfDepth = Depth / 2f;

        // Vertici del box centrato sull'origine
        var frontBottomLeft = new Vector3(-halfWidth, -halfHeight, halfDepth);
        var frontBottomRight = new Vector3(halfWidth, -halfHeight, halfDepth);
        var frontTopLeft = new Vector3(-halfWidth, halfHeight, halfDepth);
        var frontTopRight = new Vector3(halfWidth, halfHeight, halfDepth);

        var backBottomLeft = new Vector3(-halfWidth, -halfHeight, -halfDepth);
        var backBottomRight = new Vector3(halfWidth, -halfHeight, -halfDepth);
        var backTopLeft = new Vector3(-halfWidth, halfHeight, -halfDepth);
        var backTopRight = new Vector3(halfWidth, halfHeight, -halfDepth);

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

            // Left (-X)
            new(backBottomLeft, -Vector3.UnitX, uvBottomLeft),
            new(frontBottomLeft, -Vector3.UnitX, uvBottomRight),
            new(backTopLeft, -Vector3.UnitX, uvTopLeft),

            new(frontBottomLeft, -Vector3.UnitX, uvBottomRight),
            new(frontTopLeft, -Vector3.UnitX, uvTopRight),
            new(backTopLeft, -Vector3.UnitX, uvTopLeft),

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
            new(frontBottomRight, -Vector3.UnitY, uvBottomRight)
        ];
    }

    private void RebuildGeometry()
    {
        _boxVertices = CreateBoxVertices();
        _vertexBuffer.Dispose();
        _vertexBuffer = new(_graphicsDevice, _boxVertices, BufferUsage.DynamicCopy);
        _needsRebuild = false;
    }

    private static Vector4 ToVector4(Color4b color)
    {
        const float inv = 1f / 255f;

        return new(color.R * inv, color.G * inv, color.B * inv, color.A * inv);
    }

    protected override void DrawShadowGeometry(ShaderProgram shadowShader)
    {
        if (_vertexBuffer == null)
        {
            return;
        }

        if (_needsRebuild)
        {
            RebuildGeometry();
        }

        _graphicsDevice.ShaderProgram = shadowShader;
        _graphicsDevice.VertexArray = _vertexBuffer;
        _graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, _vertexBuffer.StorageLength);
    }
}
