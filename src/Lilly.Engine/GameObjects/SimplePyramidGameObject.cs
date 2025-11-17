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

public class SimplePyramidGameObject : BaseGameObject3D
{
    private VertexBuffer<VertexColor> vertexBuffer;
    private SimpleShaderProgram shaderProgram;
    private VertexColor[] pyramidVertices;
    private double lastColorChangeTime;

    public SimplePyramidGameObject(RenderContext context) : base(context.GraphicsDevice) { }

    public override void Initialize()
    {
        GeneratePyramid(0.5f);

        vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, pyramidVertices, BufferUsage.DynamicCopy);

        shaderProgram = SimpleShaderProgram.Create<VertexColor>(GraphicsDevice);
    }

    private void GeneratePyramid(float size)
    {
        var vertices = new List<VertexColor>();

        // Square base vertices
        Vector3 v0 = new Vector3(-size, -size, -size);  // Bottom-left-front
        Vector3 v1 = new Vector3(size, -size, -size);   // Bottom-right-front
        Vector3 v2 = new Vector3(size, -size, size);    // Bottom-right-back
        Vector3 v3 = new Vector3(-size, -size, size);   // Bottom-left-back

        // Pyramid apex
        Vector3 apex = new Vector3(0, size, 0);

        // Base (2 triangles)
        // Triangle 1
        vertices.Add(new VertexColor(v0, Color4b.Red));
        vertices.Add(new VertexColor(v1, Color4b.Red));
        vertices.Add(new VertexColor(v2, Color4b.Red));

        // Triangle 2
        vertices.Add(new VertexColor(v0, Color4b.Red));
        vertices.Add(new VertexColor(v2, Color4b.Red));
        vertices.Add(new VertexColor(v3, Color4b.Red));

        // Front face
        vertices.Add(new VertexColor(v0, Color4b.Green));
        vertices.Add(new VertexColor(apex, Color4b.Green));
        vertices.Add(new VertexColor(v1, Color4b.Green));

        // Right face
        vertices.Add(new VertexColor(v1, Color4b.Blue));
        vertices.Add(new VertexColor(apex, Color4b.Blue));
        vertices.Add(new VertexColor(v2, Color4b.Blue));

        // Back face
        vertices.Add(new VertexColor(v2, Color4b.Yellow));
        vertices.Add(new VertexColor(apex, Color4b.Yellow));
        vertices.Add(new VertexColor(v3, Color4b.Yellow));

        // Left face
        vertices.Add(new VertexColor(v3, Color4b.Magenta));
        vertices.Add(new VertexColor(apex, Color4b.Magenta));
        vertices.Add(new VertexColor(v0, Color4b.Magenta));

        pyramidVertices = vertices.ToArray();
    }

    private void UpdateRandomColors()
    {
        var random = new Random();
        for (int i = 0; i < pyramidVertices.Length; i++)
        {
            byte r = (byte)random.Next(256);
            byte g = (byte)random.Next(256);
            byte b = (byte)random.Next(256);
            pyramidVertices[i] = new VertexColor(pyramidVertices[i].Position, new Color4b(r, g, b, 255));
        }
    }

    /// <summary>
    /// Renders the pyramid by returning a draw command with all vertices.
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <returns>A render command for drawing the pyramid</returns>

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        yield return new RenderCommand(
            RenderCommandType.DrawArray,
            new DrawArrayPayload(
                shaderProgram,
                vertexBuffer,
                (uint)pyramidVertices.Length
            )
        );
    }

    /// <summary>
    /// Updates the pyramid every frame: changes colors, scales, and rotates it.
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public override void Update(GameTime gameTime)
    {
        double currentTime = gameTime.GetTotalGameTimeSeconds();

        // Change colors every second
        if (currentTime - lastColorChangeTime >= 1.0)
        {
            UpdateRandomColors();
            vertexBuffer.Dispose();
            vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, pyramidVertices, BufferUsage.DynamicCopy);
            lastColorChangeTime = currentTime;
        }

        // Apply sinusoidal scaling (increased base size to make pyramids more visible)
        float scale = (float)((MathF.Sin((float)currentTime * 2.0f) + 1.0f) * 1.5f + 1.5f);
        Transform.Scale = new Vector3D<float>(scale, scale, scale);

        // Apply continuous rotation
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, 0.01f);
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitX, 0.005f);
        base.Update(gameTime);
    }

    /// <summary>
    /// Sets up the shader matrices and renders the pyramid.
    /// </summary>
    /// <param name="camera">The active camera</param>
    /// <param name="gameTime">Game timing information</param>
    public override void Draw(ICamera3D camera, GameTime gameTime)
    {
        shaderProgram.Projection = camera.Projection.ToSystem();
        shaderProgram.World = Transform.GetTransformationMatrix().ToSystem();
        shaderProgram.View = camera.View.ToSystem();
        base.Draw(camera, gameTime);
    }
}
