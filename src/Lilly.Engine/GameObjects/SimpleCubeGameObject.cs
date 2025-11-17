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

public class SimpleCubeGameObject : BaseGameObject3D
{
    private VertexBuffer<VertexColor> vertexBuffer;
    private SimpleShaderProgram shaderProgram;
    private VertexColor[] cubeVertices;
    private double lastColorChangeTime;

    public SimpleCubeGameObject(RenderContext context) : base(context.GraphicsDevice) { }

    public override void Initialize()
    {
        // Initialize cube vertices with positions (colors will be set randomly)
        cubeVertices = new VertexColor[]
        {
            new VertexColor(new Vector3(-0.5f, -0.5f, -0.5f), Color4b.LightBlue), //4
            new VertexColor(new Vector3(-0.5f, -0.5f, 0.5f), Color4b.Lime),       //3
            new VertexColor(new Vector3(-0.5f, 0.5f, -0.5f), Color4b.White),      //7
            new VertexColor(new Vector3(-0.5f, 0.5f, 0.5f), Color4b.Black),       //8
            new VertexColor(new Vector3(0.5f, 0.5f, 0.5f), Color4b.Blue),         //5
            new VertexColor(new Vector3(-0.5f, -0.5f, 0.5f), Color4b.Lime),       //3
            new VertexColor(new Vector3(0.5f, -0.5f, 0.5f), Color4b.Red),         //1
            new VertexColor(new Vector3(-0.5f, -0.5f, -0.5f), Color4b.LightBlue), //4
            new VertexColor(new Vector3(0.5f, -0.5f, -0.5f), Color4b.Yellow),     //2
            new VertexColor(new Vector3(-0.5f, 0.5f, -0.5f), Color4b.White),      //7
            new VertexColor(new Vector3(0.5f, 0.5f, -0.5f), Color4b.Pink),        //6
            new VertexColor(new Vector3(0.5f, 0.5f, 0.5f), Color4b.Blue),         //5
            new VertexColor(new Vector3(0.5f, -0.5f, -0.5f), Color4b.Yellow),     //2
            new VertexColor(new Vector3(0.5f, -0.5f, 0.5f), Color4b.Red),         //1
        };

        // Generate initial random colors
        UpdateRandomColors();

        vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, cubeVertices, BufferUsage.DynamicCopy);

        shaderProgram = SimpleShaderProgram.Create<VertexColor>(GraphicsDevice);
    }

    private void UpdateRandomColors()
    {
        var random = new Random();
        for (int i = 0; i < cubeVertices.Length; i++)
        {
            // Generate random color
            byte r = (byte)random.Next(256);
            byte g = (byte)random.Next(256);
            byte b = (byte)random.Next(256);
            byte a = (byte)random.Next(256);
            cubeVertices[i] = new VertexColor(cubeVertices[i].Position, new Color4b(r, g, b, a));
        }
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        yield return new RenderCommand(
            RenderCommandType.DrawArray,
            new DrawArrayPayload(
                shaderProgram,
                vertexBuffer,
                vertexBuffer.StorageLength
            )
        );
    }

    public override void Update(GameTime gameTime)
    {
        double currentTime = gameTime.GetTotalGameTimeSeconds();

        // Change colors every second
        if (currentTime - lastColorChangeTime >= 1.0)
        {
            UpdateRandomColors();
            // Recreate vertex buffer with new colors
            vertexBuffer.Dispose();
            vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, cubeVertices, BufferUsage.DynamicCopy);
            lastColorChangeTime = currentTime;
        }

        // Scale sin from 0.5 to 1.5
        float scale = (float)((MathF.Sin((float)currentTime * 2.0f) + 1.0f) * 1.5f + 0.5f);
       // Transform.Scale = new Vector3D<float>(scale, scale, scale);
       // Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, 0.01f);
       // Transform.Rotation *= Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitZ, 0.01f);
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
