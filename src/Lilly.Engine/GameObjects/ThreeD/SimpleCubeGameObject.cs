using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Extensions;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Services;
using TrippyGL;

namespace Lilly.Engine.GameObjects.ThreeD;

/// <summary>
/// Simple animated cube that randomizes its vertex colors over time.
/// </summary>
public class SimpleCubeGameObject : Base3dGameObject, IInitializable, IUpdateble, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IAssetManager _assetManager;

    private VertexBuffer<VertexColorTexture> _vertexBuffer;
    private ShaderProgram? _shaderProgram;
    private VertexColorTexture[] _cubeVertices = [];
    private Texture2D? _texture;

    public float XRotationSpeed { get; set; }
    public float YRotationSpeed { get; set; } = 0.01f;
    public float ZRotationSpeed { get; set; }

    public SimpleCubeGameObject(
        GraphicsDevice graphicsDevice,
        IRenderPipeline gameObjectManager,
        IAssetManager assetManager
    ) : base("SimpleCube", gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
    }

    public void Initialize()
    {
        _cubeVertices = CreateCubeVertices();
        var randomBoxTexture = Random.Shared.Next(1, 5);
        _texture = _assetManager.GetTexture<Texture2D>($"box{randomBoxTexture}");

        _vertexBuffer = new VertexBuffer<VertexColorTexture>(_graphicsDevice, _cubeVertices, BufferUsage.DynamicCopy);

        _shaderProgram = _assetManager.GetShaderProgram("simple_cube");
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

        graphicsDevice.VertexArray = _vertexBuffer;
        graphicsDevice.BlendState = BlendState.NonPremultiplied;

        graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, _vertexBuffer.StorageLength);
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }

        Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, YRotationSpeed);
        Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, ZRotationSpeed);
        Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, XRotationSpeed);

        base.Update(gameTime);
    }

    private static VertexColorTexture[] CreateCubeVertices()
    {
        var frontColor = Color4b.White;
        var backColor = Color4b.White;
        var leftColor = Color4b.White.ApplyAlpha(0.3f);
        var rightColor = Color4b.White;
        var topColor = Color4b.White;
        var bottomColor = Color4b.White;

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
            new(frontBottomLeft, frontColor, uvBottomLeft),
            new(frontBottomRight, frontColor, uvBottomRight),
            new(frontTopLeft, frontColor, uvTopLeft),

            new(frontBottomRight, frontColor, uvBottomRight),
            new(frontTopRight, frontColor, uvTopRight),
            new(frontTopLeft, frontColor, uvTopLeft),

            // Back (-Z)
            new(backBottomRight, backColor, uvBottomLeft),
            new(backBottomLeft, backColor, uvBottomRight),
            new(backTopRight, backColor, uvTopLeft),

            new(backBottomLeft, backColor, uvBottomRight),
            new(backTopLeft, backColor, uvTopRight),
            new(backTopRight, backColor, uvTopLeft),

            // Right (+X)
            new(frontBottomRight, rightColor, uvBottomLeft),
            new(backBottomRight, rightColor, uvBottomRight),
            new(frontTopRight, rightColor, uvTopLeft),

            new(backBottomRight, rightColor, uvBottomRight),
            new(backTopRight, rightColor, uvTopRight),
            new(frontTopRight, rightColor, uvTopLeft),

            // Top (+Y)
            new(backTopLeft, topColor, uvTopLeft),
            new(frontTopLeft, topColor, uvBottomLeft),
            new(backTopRight, topColor, uvTopRight),

            new(frontTopLeft, topColor, uvBottomLeft),
            new(frontTopRight, topColor, uvBottomRight),
            new(backTopRight, topColor, uvTopRight),

            // Bottom (-Y)
            new(backBottomLeft, bottomColor, uvTopLeft),
            new(backBottomRight, bottomColor, uvTopRight),
            new(frontBottomLeft, bottomColor, uvBottomLeft),

            new(frontBottomLeft, bottomColor, uvBottomLeft),
            new(backBottomRight, bottomColor, uvTopRight),
            new(frontBottomRight, bottomColor, uvBottomRight),

            // Left (-X) - Moved to end for transparency
            new(backBottomLeft, leftColor, uvBottomLeft),
            new(frontBottomLeft, leftColor, uvBottomRight),
            new(backTopLeft, leftColor, uvTopLeft),

            new(frontBottomLeft, leftColor, uvBottomRight),
            new(frontTopLeft, leftColor, uvTopRight),
            new(backTopLeft, leftColor, uvTopLeft)
        ];
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();

        // Do not dispose _shaderProgram here as it is managed by AssetManager
        GC.SuppressFinalize(this);
    }
}
