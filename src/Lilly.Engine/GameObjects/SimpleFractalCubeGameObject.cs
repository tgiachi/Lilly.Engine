using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Types;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.GameObjects;

public class SimpleFractalCubeGameObject : BaseGameObject3D
{
    private VertexBuffer<VertexColor> vertexBuffer;
    private SimpleShaderProgram shaderProgram;
    private VertexColor[] fractalVertices;
    private double lastColorChangeTime;
    private readonly int _fractalLevel;

    public SimpleFractalCubeGameObject(RenderContext context) : base(context.GraphicsDevice) 
    {
        _fractalLevel = 2; // Level 2 is a good balance between detail and performance
    }

    public override void Initialize()
    {
        GenerateMengerSponge(1.0f, _fractalLevel);

        vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, fractalVertices, BufferUsage.DynamicCopy);

        shaderProgram = SimpleShaderProgram.Create<VertexColor>(GraphicsDevice);
    }

    private void GenerateMengerSponge(float size, int level)
    {
        var vertices = new List<VertexColor>();
        GenerateMengerSpongeRecursive(vertices, Vector3.Zero, size, level, 0);
        fractalVertices = vertices.ToArray();
    }

    private void GenerateMengerSpongeRecursive(List<VertexColor> vertices, Vector3 center, float size, int level, int depth)
    {
        if (level == 0)
        {
            // Base case: draw a cube
            AddCube(vertices, center, size, depth);
            return;
        }

        // Divide the cube into 27 smaller cubes (3x3x3)
        float newSize = size / 3.0f;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    // Skip the center cubes (create the holes)
                    int axisCount = (x == 0 ? 1 : 0) + (y == 0 ? 1 : 0) + (z == 0 ? 1 : 0);
                    if (axisCount >= 2)
                        continue;

                    Vector3 offset = new Vector3(x * newSize, y * newSize, z * newSize);
                    Vector3 newCenter = center + offset;

                    GenerateMengerSpongeRecursive(vertices, newCenter, newSize, level - 1, depth + 1);
                }
            }
        }
    }

    private void AddCube(List<VertexColor> vertices, Vector3 center, float size, int depth)
    {
        float halfSize = size / 2.0f;

        // Define the 8 vertices of the cube
        Vector3[] cubeVertices = new Vector3[8]
        {
            center + new Vector3(-halfSize, -halfSize, -halfSize),
            center + new Vector3(halfSize, -halfSize, -halfSize),
            center + new Vector3(halfSize, halfSize, -halfSize),
            center + new Vector3(-halfSize, halfSize, -halfSize),
            center + new Vector3(-halfSize, -halfSize, halfSize),
            center + new Vector3(halfSize, -halfSize, halfSize),
            center + new Vector3(halfSize, halfSize, halfSize),
            center + new Vector3(-halfSize, halfSize, halfSize),
        };

        // Color based on depth level for visual variety
        Color4b color = GetColorFromDepth(depth);

        // Define the 6 faces (12 triangles)
        // Front face
        AddTriangle(vertices, cubeVertices[0], cubeVertices[1], cubeVertices[2], color);
        AddTriangle(vertices, cubeVertices[0], cubeVertices[2], cubeVertices[3], color);

        // Back face
        AddTriangle(vertices, cubeVertices[5], cubeVertices[4], cubeVertices[7], color);
        AddTriangle(vertices, cubeVertices[5], cubeVertices[7], cubeVertices[6], color);

        // Left face
        AddTriangle(vertices, cubeVertices[4], cubeVertices[0], cubeVertices[3], color);
        AddTriangle(vertices, cubeVertices[4], cubeVertices[3], cubeVertices[7], color);

        // Right face
        AddTriangle(vertices, cubeVertices[1], cubeVertices[5], cubeVertices[6], color);
        AddTriangle(vertices, cubeVertices[1], cubeVertices[6], cubeVertices[2], color);

        // Top face
        AddTriangle(vertices, cubeVertices[3], cubeVertices[2], cubeVertices[6], color);
        AddTriangle(vertices, cubeVertices[3], cubeVertices[6], cubeVertices[7], color);

        // Bottom face
        AddTriangle(vertices, cubeVertices[4], cubeVertices[5], cubeVertices[1], color);
        AddTriangle(vertices, cubeVertices[4], cubeVertices[1], cubeVertices[0], color);
    }

    private void AddTriangle(List<VertexColor> vertices, Vector3 v1, Vector3 v2, Vector3 v3, Color4b color)
    {
        vertices.Add(new VertexColor(v1, color));
        vertices.Add(new VertexColor(v2, color));
        vertices.Add(new VertexColor(v3, color));
    }

    private Color4b GetColorFromDepth(int depth)
    {
        // Create a color gradient based on fractal depth
        float t = (float)depth / (_fractalLevel + 1);

        byte r = (byte)(255 * (0.3f + 0.7f * t));
        byte g = (byte)(255 * (0.5f + 0.5f * MathF.Sin(t * MathF.PI)));
        byte b = (byte)(255 * (0.8f - 0.5f * t));

        return new Color4b(r, g, b, 255);
    }

    private void UpdateShiftingColors(double time)
    {
        for (int i = 0; i < fractalVertices.Length; i += 3)
        {
            float wave = (float)(0.5f + 0.5f * MathF.Sin((float)time * 2.0f + i * 0.01f));
            
            byte r = (byte)(128 + 127 * MathF.Sin(wave * MathF.PI));
            byte g = (byte)(128 + 127 * MathF.Cos(wave * MathF.PI));
            byte b = (byte)(255 * (1.0f - wave));

            Color4b color = new Color4b(r, g, b, 255);

            fractalVertices[i] = new VertexColor(fractalVertices[i].Position, color);
            fractalVertices[i + 1] = new VertexColor(fractalVertices[i + 1].Position, color);
            fractalVertices[i + 2] = new VertexColor(fractalVertices[i + 2].Position, color);
        }
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        yield return new RenderCommand(
            RenderCommandType.DrawArray,
            new DrawArrayPayload(
                shaderProgram,
                vertexBuffer,
                (uint)fractalVertices.Length
            )
        );
    }

    public override void Update(GameTime gameTime)
    {
        double currentTime = gameTime.GetTotalGameTimeSeconds();

        // Update colors every frame for mesmerizing effect
        if (currentTime - lastColorChangeTime >= 0.05)
        {
            UpdateShiftingColors(currentTime);
            vertexBuffer.Dispose();
            vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, fractalVertices, BufferUsage.DynamicCopy);
            lastColorChangeTime = currentTime;
        }

        // Slow, steady rotation on all axes
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, 0.008f);
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitX, 0.005f);
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitZ, 0.003f);

        // Gentle pulsing scale
        float scale = (float)((MathF.Sin((float)currentTime * 0.5f) + 1.0f) * 0.3f + 1.5f);
        Transform.Scale = new Vector3D<float>(scale, scale, scale);

        base.Update(gameTime);
    }

    public override void Draw(ICamera3D camera, GameTime gameTime)
    {
        shaderProgram.Projection = camera.Projection.ToSystem();
        shaderProgram.World = Transform.GetTransformationMatrix().ToSystem();
        shaderProgram.View = camera.View.ToSystem();
        base.Draw(camera, gameTime);
    }
}
