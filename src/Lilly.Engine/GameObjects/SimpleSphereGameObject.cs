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
        // Genera la sfera con latitudine e longitudine
        GenerateSphere(16, 16, 0.5f); // 16 stacks, 16 slices, raggio 0.5

        vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, sphereVertices, BufferUsage.DynamicCopy);

        shaderProgram = SimpleShaderProgram.Create<VertexColor>(GraphicsDevice);
    }

    private void GenerateSphere(int stacks, int slices, float radius)
    {
        var vertices = new List<VertexColor>();

        // Genera vertici della sfera usando coordinate sferiche
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

                // Calcola la posizione del vertice
                float x = radius * sinPhi * cosTheta;
                float y = radius * cosPhi;
                float z = radius * sinPhi * sinTheta;

                // Colore basato sulla posizione
                byte r = (byte)((x + radius) / (2 * radius) * 255);
                byte g = (byte)((y + radius) / (2 * radius) * 255);
                byte b = (byte)((z + radius) / (2 * radius) * 255);

                vertices.Add(new VertexColor(new Vector3(x, y, z), new Color4b(r, g, b, 255)));
            }
        }

        // Crea i triangoli
        var triangles = new List<VertexColor>();
        for (int stack = 0; stack < stacks; stack++)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                int v0 = stack * (slices + 1) + slice;
                int v1 = v0 + 1;
                int v2 = v0 + (slices + 1);
                int v3 = v2 + 1;

                // Primo triangolo
                triangles.Add(vertices[v0]);
                triangles.Add(vertices[v2]);
                triangles.Add(vertices[v1]);

                // Secondo triangolo
                triangles.Add(vertices[v1]);
                triangles.Add(vertices[v2]);
                triangles.Add(vertices[v3]);
            }
        }

        sphereVertices = triangles.ToArray();
        vertexCount = sphereVertices.Length;
    }

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

    public override void Update(GameTime gameTime)
    {
        double currentTime = gameTime.GetTotalGameTimeSeconds();

        // Cambia colori ogni secondo
        if (currentTime - lastColorChangeTime >= 1.0)
        {
            UpdateRandomColors();
            vertexBuffer.Dispose();
            vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, sphereVertices, BufferUsage.DynamicCopy);
            lastColorChangeTime = currentTime;
        }

        // Scala con seno
        float scale = (float)((MathF.Sin((float)currentTime * 2.0f) + 1.0f) * 0.5f + 0.5f);
        Transform.Scale = new Vector3D<float>(scale, scale, scale);

        // Ruota continuamente
        Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, 0.01f);
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
