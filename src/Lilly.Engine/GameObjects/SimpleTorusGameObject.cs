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

public class SimpleTorusGameObject : BaseGameObject3D
{
    private VertexBuffer<VertexColor> vertexBuffer;
    private SimpleShaderProgram shaderProgram;
    private VertexColor[] torusVertices;
    private double lastColorChangeTime;

    public SimpleTorusGameObject(RenderContext context) : base(context.GraphicsDevice) { }

    public override void Initialize()
    {
        GenerateTorus(0.5f, 0.2f, 32, 16);

        vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, torusVertices, BufferUsage.DynamicCopy);

        shaderProgram = SimpleShaderProgram.Create<VertexColor>(GraphicsDevice);
    }

    private void GenerateTorus(float majorRadius, float minorRadius, int majorSegments, int minorSegments)
    {
        var vertices = new List<VertexColor>();

        for (int i = 0; i < majorSegments; i++)
        {
            float theta = i * MathF.PI * 2.0f / majorSegments;
            float nextTheta = (i + 1) * MathF.PI * 2.0f / majorSegments;

            for (int j = 0; j < minorSegments; j++)
            {
                float phi = j * MathF.PI * 2.0f / minorSegments;
                float nextPhi = (j + 1) * MathF.PI * 2.0f / minorSegments;

                // Calculate the four corners of the current quad
                Vector3 v1 = CalculateTorusVertex(theta, phi, majorRadius, minorRadius);
                Vector3 v2 = CalculateTorusVertex(nextTheta, phi, majorRadius, minorRadius);
                Vector3 v3 = CalculateTorusVertex(nextTheta, nextPhi, majorRadius, minorRadius);
                Vector3 v4 = CalculateTorusVertex(theta, nextPhi, majorRadius, minorRadius);

                // Color gradient based on position
                Color4b color1 = GetColorFromPosition(i, j, majorSegments, minorSegments);
                Color4b color2 = GetColorFromPosition(i + 1, j, majorSegments, minorSegments);
                Color4b color3 = GetColorFromPosition(i + 1, j + 1, majorSegments, minorSegments);
                Color4b color4 = GetColorFromPosition(i, j + 1, majorSegments, minorSegments);

                // First triangle
                vertices.Add(new VertexColor(v1, color1));
                vertices.Add(new VertexColor(v2, color2));
                vertices.Add(new VertexColor(v3, color3));

                // Second triangle
                vertices.Add(new VertexColor(v1, color1));
                vertices.Add(new VertexColor(v3, color3));
                vertices.Add(new VertexColor(v4, color4));
            }
        }

        torusVertices = vertices.ToArray();
    }

    private Vector3 CalculateTorusVertex(float theta, float phi, float majorRadius, float minorRadius)
    {
        float x = (majorRadius + minorRadius * MathF.Cos(phi)) * MathF.Cos(theta);
        float y = minorRadius * MathF.Sin(phi);
        float z = (majorRadius + minorRadius * MathF.Cos(phi)) * MathF.Sin(theta);

        return new Vector3(x, y, z);
    }

    private Color4b GetColorFromPosition(int i, int j, int majorSegments, int minorSegments)
    {
        float hue = (float)i / majorSegments;
        float saturation = (float)j / minorSegments;

        byte r = (byte)(255 * hue);
        byte g = (byte)(255 * saturation);
        byte b = (byte)(255 * (1.0f - hue));

        return new Color4b(r, g, b, 255);
    }

    private void UpdateRandomColors()
    {
        var random = new Random();
        for (int i = 0; i < torusVertices.Length; i++)
        {
            byte r = (byte)random.Next(256);
            byte g = (byte)random.Next(256);
            byte b = (byte)random.Next(256);
            torusVertices[i] = new VertexColor(torusVertices[i].Position, new Color4b(r, g, b, 255));
        }
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        yield return new RenderCommand(
            RenderCommandType.DrawArray,
            new DrawArrayPayload(
                shaderProgram,
                vertexBuffer,
                (uint)torusVertices.Length
            )
        );
    }

    public override void Update(GameTime gameTime)
    {
        double currentTime = gameTime.GetTotalGameTimeSeconds();

        // Change colors every 2 seconds
        if (currentTime - lastColorChangeTime >= 2.0)
        {
            UpdateRandomColors();
            vertexBuffer.Dispose();
            vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, torusVertices, BufferUsage.DynamicCopy);
            lastColorChangeTime = currentTime;
        }

        // Apply sinusoidal scaling
        float scale = (float)((MathF.Sin((float)currentTime * 1.5f) + 1.0f) * 1.0f + 1.0f);
        Transform.Scale = new Vector3D<float>(scale, scale, scale);

        // Rotate on multiple axes for visual effect
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, 0.015f);
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitX, 0.008f);
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitZ, 0.005f);

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
