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
/// Simple plane object that can be used as a ground, wall, or any flat surface.
/// </summary>
public class SimplePlaneGameObject : Base3dGameObject, IInitializable, IUpdateble, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IAssetManager _assetManager;

    private VertexBuffer<VertexColorTexture> _vertexBuffer;
    private ShaderProgram? _shaderProgram;
    private VertexColorTexture[] _planeVertices = [];
    private Texture2D? _texture;

    public float Width { get; set; } = 1f;
    public float Depth { get; set; } = 1f;
    public string TextureName { get; set; } = "box1";
    public Color4b PlaneColor { get; set; } = Color4b.White;

    public SimplePlaneGameObject(
        GraphicsDevice graphicsDevice,
        IRenderPipeline gameObjectManager,
        IAssetManager assetManager,
        float width = 1f,
        float depth = 1f
    ) : base("SimplePlane", gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
        Width = width;
        Depth = depth;
    }

    public void Initialize()
    {
        _planeVertices = CreatePlaneVertices();
        _texture = _assetManager.GetTexture<Texture2D>(TextureName);

        _vertexBuffer = new VertexBuffer<VertexColorTexture>(_graphicsDevice, _planeVertices, BufferUsage.DynamicCopy);

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

    private VertexColorTexture[] CreatePlaneVertices()
    {
        var halfWidth = Width / 2f;
        var halfDepth = Depth / 2f;

        // Piano sul piano XZ (orizzontale, Y = 0)
        var bottomLeft = new Vector3(-halfWidth, 0f, -halfDepth);
        var bottomRight = new Vector3(halfWidth, 0f, -halfDepth);
        var topLeft = new Vector3(-halfWidth, 0f, halfDepth);
        var topRight = new Vector3(halfWidth, 0f, halfDepth);

        var uvTopLeft = new Vector2(0, 0);
        var uvTopRight = new Vector2(1, 0);
        var uvBottomLeft = new Vector2(0, 1);
        var uvBottomRight = new Vector2(1, 1);

        return
        [
            // Face superiore (+Y) - visibile dall'alto
            new(bottomLeft, PlaneColor, uvBottomLeft),
            new(bottomRight, PlaneColor, uvBottomRight),
            new(topLeft, PlaneColor, uvTopLeft),

            new(bottomRight, PlaneColor, uvBottomRight),
            new(topRight, PlaneColor, uvTopRight),
            new(topLeft, PlaneColor, uvTopLeft),

            // Face inferiore (-Y) - visibile dal basso
            new(topLeft, PlaneColor, uvTopLeft),
            new(topRight, PlaneColor, uvTopRight),
            new(bottomLeft, PlaneColor, uvBottomLeft),

            new(topRight, PlaneColor, uvTopRight),
            new(bottomRight, PlaneColor, uvBottomRight),
            new(bottomLeft, PlaneColor, uvBottomLeft)
        ];
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();

        // Do not dispose _shaderProgram here as it is managed by AssetManager
        GC.SuppressFinalize(this);
    }
}
