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

public class SimpleStarGameObject : BaseGameObject3D
{
    private VertexBuffer<VertexColor> vertexBuffer;
    private SimpleShaderProgram shaderProgram;
    private VertexColor[] starVertices;
    private double lastColorChangeTime;

    public SimpleStarGameObject(RenderContext context) : base(context.GraphicsDevice) { }

    public override void Initialize()
    {
        GenerateStar(0.5f, 0.2f, 5, 8);

        vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, starVertices, BufferUsage.DynamicCopy);

        shaderProgram = SimpleShaderProgram.Create<VertexColor>(GraphicsDevice);
    }

    private void GenerateStar(float outerRadius, float innerRadius, int numPoints, int segments)
    {
        var vertices = new List<VertexColor>();
        Vector3 center = Vector3.Zero;

        // Generate star points in a circle
        var outerPoints = new List<Vector3>();
        var innerPoints = new List<Vector3>();

        for (int i = 0; i < numPoints * 2; i++)
        {
            float angle = i * MathF.PI / numPoints;
            float radius = (i % 2 == 0) ? outerRadius : innerRadius;

            float x = radius * MathF.Cos(angle);
            float z = radius * MathF.Sin(angle);

            if (i % 2 == 0)
                outerPoints.Add(new Vector3(x, 0, z));
            else
                innerPoints.Add(new Vector3(x, 0, z));
        }

        // Create top pyramid (outer points to top apex)
        Vector3 topApex = new Vector3(0, outerRadius * 0.7f, 0);
        for (int i = 0; i < numPoints; i++)
        {
            int nextI = (i + 1) % numPoints;

            Color4b color = GetColorFromIndex(i, numPoints);

            // Outer face
            vertices.Add(new VertexColor(outerPoints[i], color));
            vertices.Add(new VertexColor(topApex, color));
            vertices.Add(new VertexColor(outerPoints[nextI], color));

            // Inner face
            vertices.Add(new VertexColor(innerPoints[i], color));
            vertices.Add(new VertexColor(topApex, color));
            vertices.Add(new VertexColor(outerPoints[i], color));
        }

        // Create bottom pyramid (outer points to bottom apex)
        Vector3 bottomApex = new Vector3(0, -outerRadius * 0.7f, 0);
        for (int i = 0; i < numPoints; i++)
        {
            int nextI = (i + 1) % numPoints;

            Color4b color = GetColorFromIndex(i + numPoints, numPoints * 2);

            // Outer face
            vertices.Add(new VertexColor(outerPoints[i], color));
            vertices.Add(new VertexColor(outerPoints[nextI], color));
            vertices.Add(new VertexColor(bottomApex, color));

            // Inner face
            vertices.Add(new VertexColor(innerPoints[i], color));
            vertices.Add(new VertexColor(outerPoints[i], color));
            vertices.Add(new VertexColor(bottomApex, color));
        }

        // Connect top and bottom (side faces between inner and outer)
        for (int i = 0; i < numPoints; i++)
        {
            int nextI = (i + 1) % numPoints;

            Color4b color = GetColorFromIndex(i, numPoints);

            // Outer edge
            vertices.Add(new VertexColor(outerPoints[i], color));
            vertices.Add(new VertexColor(outerPoints[nextI], color));
            vertices.Add(new VertexColor(innerPoints[i], color));

            vertices.Add(new VertexColor(innerPoints[i], color));
            vertices.Add(new VertexColor(outerPoints[nextI], color));
            vertices.Add(new VertexColor(innerPoints[nextI], color));
        }

        starVertices = vertices.ToArray();
    }

    private Color4b GetColorFromIndex(int index, int total)
    {
        float hue = (float)index / total;
        
        byte r = (byte)(255 * (0.5f + 0.5f * MathF.Sin(hue * MathF.PI * 2.0f)));
        byte g = (byte)(255 * (0.5f + 0.5f * MathF.Sin(hue * MathF.PI * 2.0f + MathF.PI * 2.0f / 3.0f)));
        byte b = (byte)(255 * (0.5f + 0.5f * MathF.Sin(hue * MathF.PI * 2.0f + MathF.PI * 4.0f / 3.0f)));

        return new Color4b(r, g, b, 255);
    }

    private void UpdatePulsingColors(double time)
    {
        for (int i = 0; i < starVertices.Length; i++)
        {
            float pulse = (float)(0.5f + 0.5f * MathF.Sin((float)time * 3.0f + i * 0.1f));
            
            byte r = (byte)(255 * pulse);
            byte g = (byte)(255 * (1.0f - pulse));
            byte b = (byte)(128 + 127 * pulse);

            starVertices[i] = new VertexColor(starVertices[i].Position, new Color4b(r, g, b, 255));
        }
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        yield return new RenderCommand(
            RenderCommandType.DrawArray,
            new DrawArrayPayload(
                shaderProgram,
                vertexBuffer,
                (uint)starVertices.Length
            )
        );
    }

    public override void Update(GameTime gameTime)
    {
        double currentTime = gameTime.GetTotalGameTimeSeconds();

        // Update pulsing colors every frame
        UpdatePulsingColors(currentTime);
        vertexBuffer.Dispose();
        vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, starVertices, BufferUsage.DynamicCopy);

        // Apply sinusoidal scaling with pulsing effect
        float scale = (float)((MathF.Sin((float)currentTime * 2.5f) + 1.0f) * 1.2f + 0.8f);
        Transform.Scale = new Vector3D<float>(scale, scale, scale);

        // Rotate rapidly on Y axis (like a spinning star)
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, 0.025f);
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitZ, 0.01f);

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
