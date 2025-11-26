using System;
using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Primitives;
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

    public SimpleCubeGameObject(GraphicsDevice graphicsDevice) : base("SimpleCube")
    {
        _graphicsDevice = graphicsDevice;
    }

    public void Initialize()
    {
        _cubeVertices = CreateCubeVertices();
        ApplyRandomColors();

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

        // The vertex list is arranged as a triangle strip.
        graphicsDevice.DrawArrays(PrimitiveType.TriangleStrip, 0, _vertexBuffer.StorageLength);
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }

        var currentTime = gameTime.GetTotalGameTimeSeconds();

        if (currentTime - _lastColorChangeTime >= 1.0f)
        {
            ApplyRandomColors();
            RecreateBuffer();
            _lastColorChangeTime = currentTime;
        }

        var scale = 0.5f + 0.5f * (1f + MathF.Sin(currentTime * 2.0f));
        Transform.Scale = new(scale, scale, scale);
        Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.01f);
        Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.01f);

        base.Update(gameTime);
    }

    private VertexColor[] CreateCubeVertices()
        => new VertexColor[]
        {
            new(new Vector3(-0.5f, -0.5f, -0.5f), Color4b.LightBlue), // 4
            new(new Vector3(-0.5f, -0.5f, 0.5f), Color4b.Lime),       // 3
            new(new Vector3(-0.5f, 0.5f, -0.5f), Color4b.White),      // 7
            new(new Vector3(-0.5f, 0.5f, 0.5f), Color4b.Black),       // 8
            new(new Vector3(0.5f, 0.5f, 0.5f), Color4b.Blue),         // 5
            new(new Vector3(-0.5f, -0.5f, 0.5f), Color4b.Lime),       // 3
            new(new Vector3(0.5f, -0.5f, 0.5f), Color4b.Red),         // 1
            new(new Vector3(-0.5f, -0.5f, -0.5f), Color4b.LightBlue), // 4
            new(new Vector3(0.5f, -0.5f, -0.5f), Color4b.Yellow),     // 2
            new(new Vector3(-0.5f, 0.5f, -0.5f), Color4b.White),      // 7
            new(new Vector3(0.5f, 0.5f, -0.5f), Color4b.Pink),        // 6
            new(new Vector3(0.5f, 0.5f, 0.5f), Color4b.Blue),         // 5
            new(new Vector3(0.5f, -0.5f, -0.5f), Color4b.Yellow),     // 2
            new(new Vector3(0.5f, -0.5f, 0.5f), Color4b.Red)          // 1
        };

    private void ApplyRandomColors()
    {
        for (var i = 0; i < _cubeVertices.Length; i++)
        {
            var r = (byte)_random.Next(256);
            var g = (byte)_random.Next(256);
            var b = (byte)_random.Next(256);
            var a = (byte)_random.Next(256);

            _cubeVertices[i] = new VertexColor(_cubeVertices[i].Position, new Color4b(r, g, b, a));
        }
    }

    private void RecreateBuffer()
    {
        if (_vertexBuffer == null)
        {
            _vertexBuffer = new VertexBuffer<VertexColor>(_graphicsDevice, _cubeVertices, BufferUsage.DynamicCopy);
        }
        else
        {
            // Update in-place to avoid reallocation each frame.
            _vertexBuffer.DataSubset.SetData(_cubeVertices, 0);
        }
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _shaderProgram?.Dispose();
        GC.SuppressFinalize(this);
    }
}
