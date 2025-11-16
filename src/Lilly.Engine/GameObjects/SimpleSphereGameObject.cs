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

public class SimpleSphereGameObject : BaseGameObject3D
{
    private VertexBuffer<VertexColor> vertexBuffer;
    private SimpleShaderProgram shaderProgram;
    private VertexColor[] sphereVertices;
    private double lastColorChangeTime;
    private int vertexCount;

    public SimpleSphereGameObject(RenderContext context) : base(context.GraphicsDevice) { }

    public override void Initialize()
    {
        // Generate sphere using latitude and longitude parameters
        GenerateSphere(16, 16, 0.5f); // 16 stacks, 16 slices, radius 0.5

        vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, sphereVertices, BufferUsage.DynamicCopy);

        shaderProgram = SimpleShaderProgram.Create<VertexColor>(GraphicsDevice);
    }

    /// <summary>
    /// Generates a sphere mesh using spherical coordinates (latitude/longitude).
    /// </summary>
    /// <param name="stacks">Number of horizontal subdivisions</param>
    /// <param name="slices">Number of vertical subdivisions</param>
    /// <param name="radius">Sphere radius</param>
    private void GenerateSphere(int stacks, int slices, float radius)
    {
        var vertices = new List<VertexColor>();

        // Generate vertices using spherical coordinates
        for (int stack = 0; stack <= stacks; stack++)
        {
            float phi = MathF.PI * stack / stacks;
            float sinPhi = MathF.Sin(phi);
            float cosPhi = MathF.Cos(phi);

            for (int slice = 0; slice <= slices; slice++)
            {
                float theta = 2f * MathF.PI * slice / slices;
                float sinTheta = MathF.Sin(theta);
                float cosTheta = MathF.Cos(theta);

                // Calculate vertex position
                float x = radius * sinPhi * cosTheta;
                float y = radius * cosPhi;
                float z = radius * sinPhi * sinTheta;

                // Color based on position
                byte r = (byte)((x + radius) / (2 * radius) * 255);
                byte g = (byte)((y + radius) / (2 * radius) * 255);
                byte b = (byte)((z + radius) / (2 * radius) * 255);

                vertices.Add(new VertexColor(new Vector3(x, y, z), new Color4b(r, g, b, 255)));
            }
        }

        // Create triangles from vertices
        var triangles = new List<VertexColor>();
        for (int stack = 0; stack < stacks; stack++)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                int v0 = stack * (slices + 1) + slice;
                int v1 = v0 + 1;
                int v2 = v0 + (slices + 1);
                int v3 = v2 + 1;

                // First triangle
                triangles.Add(vertices[v0]);
                triangles.Add(vertices[v2]);
                triangles.Add(vertices[v1]);

                // Second triangle
                triangles.Add(vertices[v1]);
                triangles.Add(vertices[v2]);
                triangles.Add(vertices[v3]);
            }
        }

        sphereVertices = triangles.ToArray();
        vertexCount = sphereVertices.Length;
    }

    /// <summary>
    /// Updates all vertex colors to random values.
    /// </summary>
    private void UpdateRandomColors()
    {
        var random = new Random();
        for (int i = 0; i < sphereVertices.Length; i++)
        {
            byte r = (byte)random.Next(256);
            byte g = (byte)random.Next(256);
            byte b = (byte)random.Next(256);
            sphereVertices[i] = new VertexColor(sphereVertices[i].Position, new Color4b(r, g, b, 255));
        }
    }

    /// <summary>
    /// Renders the sphere by returning a draw command with all vertices.
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <returns>A render command for drawing the sphere</returns>

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        yield return new RenderCommand(
            RenderCommandType.DrawArray,
            new DrawArrayPayload(
                shaderProgram,
                vertexBuffer,
                (uint)vertexCount
            )
        );
    }

    /// <summary>
    /// Updates the sphere every frame: changes colors, scales, and rotates it.
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
            vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, sphereVertices, BufferUsage.DynamicCopy);
            lastColorChangeTime = currentTime;
        }

        // Apply sinusoidal scaling
        float scale = (float)((MathF.Sin((float)currentTime * 2.0f) + 1.0f) * 0.5f + 0.5f);
        Transform.Scale = new Vector3D<float>(scale, scale, scale);

        // Apply continuous rotation
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, 0.01f);
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitZ, 0.005f);
        base.Update(gameTime);
    }

    /// <summary>
    /// Sets up the shader matrices and renders the sphere.
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
