using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using TrippyGL;

namespace Lilly.Engine.GameObjects.ThreeD;

/// <summary>
/// Simple capsule renderable built from triangles (hemispheres + cylinder).
/// </summary>
public class SimpleCapsuleGameObject : Base3dGameObject, IInitializable, IUpdateble, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IAssetManager _assetManager;

    private VertexBuffer<VertexColorTexture> _vertexBuffer;
    private ShaderProgram? _shaderProgram;
    private VertexColorTexture[] _vertices = [];
    private Texture2D? _texture;

    public float Radius { get; set; } = 0.5f;
    public float Height { get; set; } = 2.0f; // full height including hemispheres
    public int Slices { get; set; } = 18;
    public int HemisphereStacks { get; set; } = 8;
    public string TextureName { get; set; } = "box1";
    public Color4b CapsuleColor { get; set; } = Color4b.White;

    public SimpleCapsuleGameObject(
        GraphicsDevice graphicsDevice,
        IRenderPipeline gameObjectManager,
        IAssetManager assetManager
    ) : base("SimpleCapsule", gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
    }

    public void Initialize()
    {
        _vertices = CreateCapsuleVertices();
        _texture = _assetManager.GetTexture<Texture2D>(TextureName);

        _vertexBuffer = new VertexBuffer<VertexColorTexture>(_graphicsDevice, _vertices, BufferUsage.DynamicCopy);
        _shaderProgram = _assetManager.GetShaderProgram("simple_cube");
    }

    public override void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera)
    {
        if (!IsActive || _shaderProgram == null || _texture == null)
        {
            return;
        }

        graphicsDevice.ShaderProgram = _shaderProgram;

        _shaderProgram.Uniforms["World"].SetValueMat4(Transform.GetTransformationMatrix());
        _shaderProgram.Uniforms["View"].SetValueMat4(camera.View);
        _shaderProgram.Uniforms["Projection"].SetValueMat4(camera.Projection);
        _shaderProgram.Uniforms["Texture"].SetValueTexture(_texture);

        graphicsDevice.VertexArray = _vertexBuffer;
        graphicsDevice.BlendState = BlendState.NonPremultiplied;

        graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, _vertexBuffer.StorageLength);
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }

        base.Update(gameTime);
    }

    private VertexColorTexture[] CreateCapsuleVertices()
    {
        var cappedHeight = MathF.Max(Height, Radius * 2f);
        var halfCylinder = MathF.Max(0, cappedHeight * 0.5f - Radius);

        var rings = new List<(float y, float ringRadius)>();

        // Top hemisphere (includes equator at +halfCylinder)
        for (var i = 0; i <= HemisphereStacks; i++)
        {
            var t = i / (float)HemisphereStacks;
            var phi = t * (MathF.PI / 2f);
            var y = halfCylinder + Radius * MathF.Cos(phi);
            var r = Radius * MathF.Sin(phi);
            rings.Add((y, r));
        }

        // Bottom cylinder cap at -halfCylinder
        rings.Add((-halfCylinder, Radius));

        // Bottom hemisphere (starts after equator to avoid duplicate ring)
        for (var i = 1; i <= HemisphereStacks; i++)
        {
            var t = i / (float)HemisphereStacks;
            var phi = t * (MathF.PI / 2f);
            var y = -halfCylinder - Radius * MathF.Cos(phi);
            var r = Radius * MathF.Sin(phi);
            rings.Add((y, r));
        }

        var vertices = new List<VertexColorTexture>();
        var sliceCount = Math.Max(3, Slices);

        for (var ring = 0; ring < rings.Count - 1; ring++)
        {
            var (y0, r0) = rings[ring];
            var (y1, r1) = rings[ring + 1];

            for (var slice = 0; slice < sliceCount; slice++)
            {
                var nextSlice = (slice + 1) % sliceCount;

                var (v00, uv00) = CreateVertexOnRing(r0, y0, slice, sliceCount);
                var (v01, uv01) = CreateVertexOnRing(r0, y0, nextSlice, sliceCount);
                var (v10, uv10) = CreateVertexOnRing(r1, y1, slice, sliceCount);
                var (v11, uv11) = CreateVertexOnRing(r1, y1, nextSlice, sliceCount);

                // First triangle
                vertices.Add(new VertexColorTexture(v00, CapsuleColor, uv00));
                vertices.Add(new VertexColorTexture(v10, CapsuleColor, uv10));
                vertices.Add(new VertexColorTexture(v01, CapsuleColor, uv01));

                // Second triangle
                vertices.Add(new VertexColorTexture(v01, CapsuleColor, uv01));
                vertices.Add(new VertexColorTexture(v10, CapsuleColor, uv10));
                vertices.Add(new VertexColorTexture(v11, CapsuleColor, uv11));
            }
        }

        return vertices.ToArray();
    }

    private static (Vector3 position, Vector2 uv) CreateVertexOnRing(float radius, float y, int slice, int sliceCount)
    {
        var angle = (slice / (float)sliceCount) * MathF.Tau;
        var x = radius * MathF.Cos(angle);
        var z = radius * MathF.Sin(angle);

        var u = slice / (float)sliceCount;
        var v = 0.5f + y; // simple mapping; caller may scale object for better UVs
        return (new Vector3(x, y, z), new Vector2(u, v));
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        GC.SuppressFinalize(this);
    }
}
