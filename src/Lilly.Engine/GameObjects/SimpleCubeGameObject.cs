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
    VertexBuffer<VertexColor> vertexBuffer;
    SimpleShaderProgram shaderProgram;

    public SimpleCubeGameObject(RenderContext context) : base(context.GraphicsDevice) { }

    public override void Initialize()
    {
        Span<VertexColor> cubeBufferData = stackalloc VertexColor[]
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

        vertexBuffer = new VertexBuffer<VertexColor>(GraphicsDevice, cubeBufferData, BufferUsage.StaticCopy);

        shaderProgram = SimpleShaderProgram.Create<VertexColor>(GraphicsDevice);
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

    public override void Draw(ICamera3D camera, GameTime gameTime)
    {
        shaderProgram.Projection = camera.Projection.ToSystem();
        shaderProgram.World = Transform.GetTransformationMatrix().ToSystem();
        shaderProgram.View = camera.View.ToSystem();
        base.Draw(camera, gameTime);
    }
}
