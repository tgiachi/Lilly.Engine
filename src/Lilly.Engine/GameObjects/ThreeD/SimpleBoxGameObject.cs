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
/// Simple box object with customizable width, height, and depth dimensions.
/// </summary>
public class SimpleBoxGameObject : Base3dGameObject, IInitializable, IUpdateble, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IAssetManager _assetManager;

    private VertexBuffer<VertexColorTexture> _vertexBuffer;
    private ShaderProgram? _shaderProgram;
    private VertexColorTexture[] _boxVertices = [];
    private Texture2D? _texture;

    public float Width { get; set; } = 1f;
    public float Height { get; set; } = 1f;
    public float Depth { get; set; } = 1f;
    public string TextureName { get; set; } = "box1";
    public Color4b BoxColor { get; set; } = Color4b.White;

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

    public void Initialize()
    {
        _boxVertices = CreateBoxVertices();
        _texture = _assetManager.GetTexture<Texture2D>(TextureName);

        _vertexBuffer = new VertexBuffer<VertexColorTexture>(_graphicsDevice, _boxVertices, BufferUsage.DynamicCopy);

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

        base.Update(gameTime);
    }

    private VertexColorTexture[] CreateBoxVertices()
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
            new(frontBottomLeft, BoxColor, uvBottomLeft),
            new(frontBottomRight, BoxColor, uvBottomRight),
            new(frontTopLeft, BoxColor, uvTopLeft),

            new(frontBottomRight, BoxColor, uvBottomRight),
            new(frontTopRight, BoxColor, uvTopRight),
            new(frontTopLeft, BoxColor, uvTopLeft),

            // Back (-Z)
            new(backBottomRight, BoxColor, uvBottomLeft),
            new(backBottomLeft, BoxColor, uvBottomRight),
            new(backTopRight, BoxColor, uvTopLeft),

            new(backBottomLeft, BoxColor, uvBottomRight),
            new(backTopLeft, BoxColor, uvTopRight),
            new(backTopRight, BoxColor, uvTopLeft),

            // Right (+X)
            new(frontBottomRight, BoxColor, uvBottomLeft),
            new(backBottomRight, BoxColor, uvBottomRight),
            new(frontTopRight, BoxColor, uvTopLeft),

            new(backBottomRight, BoxColor, uvBottomRight),
            new(backTopRight, BoxColor, uvTopRight),
            new(frontTopRight, BoxColor, uvTopLeft),

            // Left (-X)
            new(backBottomLeft, BoxColor, uvBottomLeft),
            new(frontBottomLeft, BoxColor, uvBottomRight),
            new(backTopLeft, BoxColor, uvTopLeft),

            new(frontBottomLeft, BoxColor, uvBottomRight),
            new(frontTopLeft, BoxColor, uvTopRight),
            new(backTopLeft, BoxColor, uvTopLeft),

            // Top (+Y)
            new(backTopLeft, BoxColor, uvTopLeft),
            new(frontTopLeft, BoxColor, uvBottomLeft),
            new(backTopRight, BoxColor, uvTopRight),

            new(frontTopLeft, BoxColor, uvBottomLeft),
            new(frontTopRight, BoxColor, uvBottomRight),
            new(backTopRight, BoxColor, uvTopRight),

            // Bottom (-Y)
            new(backBottomLeft, BoxColor, uvTopLeft),
            new(backBottomRight, BoxColor, uvTopRight),
            new(frontBottomLeft, BoxColor, uvBottomLeft),

            new(frontBottomLeft, BoxColor, uvBottomLeft),
            new(backBottomRight, BoxColor, uvTopRight),
            new(frontBottomRight, BoxColor, uvBottomRight)
        ];
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();

        // Do not dispose _shaderProgram here as it is managed by AssetManager
        GC.SuppressFinalize(this);
    }
}
