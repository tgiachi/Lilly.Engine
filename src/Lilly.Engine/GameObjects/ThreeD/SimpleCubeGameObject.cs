using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Extensions;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Camera;
using TrippyGL;

namespace Lilly.Engine.GameObjects.ThreeD;

/// <summary>
/// Simple animated cube that randomizes its vertex colors over time.
/// </summary>
public class SimpleCubeGameObject : Base3dGameObject, IInitializable, IUpdateble, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;

    private readonly Random _random = new();
    private VertexBuffer<VertexColor> _vertexBuffer;
    private SimpleShaderProgram? _shaderProgram;
    private VertexColor[] _cubeVertices = Array.Empty<VertexColor>();
    private double _lastColorChangeTime;


    public float XRotationSpeed { get; set; } = 0.00f;
    public float YRotationSpeed { get; set; } = 0.01f;
    public float ZRotationSpeed { get; set; } = 0.00f;

    public SimpleCubeGameObject(GraphicsDevice graphicsDevice) : base("SimpleCube")
    {
        _graphicsDevice = graphicsDevice;
    }

    public void Initialize()
    {
        _cubeVertices = CreateCubeVertices();

        _vertexBuffer = new VertexBuffer<VertexColor>(_graphicsDevice, _cubeVertices, BufferUsage.DynamicCopy);
        _shaderProgram = SimpleShaderProgram.Create<VertexColor>(_graphicsDevice);
    }

    public override void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera)
    {
        if (!IsActive || _shaderProgram == null)
        {
            return;
        }

        _shaderProgram.Projection = camera.Projection;
        _shaderProgram.View = camera.View;
        _shaderProgram.World = Transform.GetTransformationMatrix();

        graphicsDevice.ShaderProgram = _shaderProgram;
        graphicsDevice.VertexArray = _vertexBuffer;

        // Draw independent triangles so each face can own its color.
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

    private static VertexColor[] CreateCubeVertices()
    {
        var frontColor = Color4b.Red;
        var backColor = Color4b.Blue.ApplyAlpha(0.8f);
        var leftColor = Color4b.Lime;
        var rightColor = Color4b.Yellow.ApplyAlpha(0.8f);
        var topColor = Color4b.White.ApplyAlpha(0.8f);
        var bottomColor = Color4b.Pink.ApplyAlpha(0.8f);

        var frontBottomLeft = new Vector3(-0.5f, -0.5f, 0.5f);
        var frontBottomRight = new Vector3(0.5f, -0.5f, 0.5f);
        var frontTopLeft = new Vector3(-0.5f, 0.5f, 0.5f);
        var frontTopRight = new Vector3(0.5f, 0.5f, 0.5f);

        var backBottomLeft = new Vector3(-0.5f, -0.5f, -0.5f);
        var backBottomRight = new Vector3(0.5f, -0.5f, -0.5f);
        var backTopLeft = new Vector3(-0.5f, 0.5f, -0.5f);
        var backTopRight = new Vector3(0.5f, 0.5f, -0.5f);

        return
        [
            // Front (+Z)
            new(frontBottomLeft, frontColor),
            new(frontBottomRight, frontColor),
            new(frontTopLeft, frontColor),

            new(frontBottomRight, frontColor),
            new(frontTopRight, frontColor),
            new(frontTopLeft, frontColor),

            // Back (-Z)
            new(backBottomRight, backColor),
            new(backBottomLeft, backColor),
            new(backTopRight, backColor),

            new(backBottomLeft, backColor),
            new(backTopLeft, backColor),
            new(backTopRight, backColor),

            // Left (-X)
            new(backBottomLeft, leftColor),
            new(frontBottomLeft, leftColor),
            new(backTopLeft, leftColor),

            new(frontBottomLeft, leftColor),
            new(frontTopLeft, leftColor),
            new(backTopLeft, leftColor),

            // Right (+X)
            new(frontBottomRight, rightColor),
            new(backBottomRight, rightColor),
            new(frontTopRight, rightColor),

            new(backBottomRight, rightColor),
            new(backTopRight, rightColor),
            new(frontTopRight, rightColor),

            // Top (+Y) with 80% alpha
            new(backTopLeft, topColor),
            new(frontTopLeft, topColor),
            new(backTopRight, topColor),

            new(frontTopLeft, topColor),
            new(frontTopRight, topColor),
            new(backTopRight, topColor),

            // Bottom (-Y) with 80% alpha
            new(backBottomLeft, bottomColor),
            new(backBottomRight, bottomColor),
            new(frontBottomLeft, bottomColor),

            new(frontBottomLeft, bottomColor),
            new(backBottomRight, bottomColor),
            new(frontBottomRight, bottomColor)
        ];
    }



    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _shaderProgram?.Dispose();
        GC.SuppressFinalize(this);
    }
}
