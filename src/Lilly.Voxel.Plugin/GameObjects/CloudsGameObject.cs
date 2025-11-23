using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Helpers;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Types;
using Lilly.Voxel.Plugin.Primitives.Vertex;
using Serilog;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

/// <summary>
/// Renders cloud instances using legacy face-based shading.
/// </summary>
public sealed class CloudsGameObject : BaseGameObject3D
{
    private readonly ILogger _logger = Log.ForContext<CloudsGameObject>();
    private readonly ShaderProgram _cloudsShader;
    private readonly IAssetManager _assetManager;

    private VertexBuffer<CloudsVertex>? _vertexBuffer;
    private uint _vertexCount;
    private float _time;
    private readonly List<Cloud> _clouds = new();

    /// <summary>
    /// Gets or sets the ambient light color.
    /// </summary>
    public Vector3D<float> AmbientLight { get; set; } = Vector3D<float>.One;

    /// <summary>
    /// Gets or sets the primary light direction.
    /// </summary>
    public Vector3D<float> LightDirection { get; set; } = new(0f, -1f, 0f);

    /// <summary>
    /// Gets the number of clouds currently registered.
    /// </summary>
    public int CloudCount => _clouds.Count;

    public CloudsGameObject(GraphicsDevice graphicsDevice, IAssetManager assetManager) : base(graphicsDevice)
    {
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));

        try
        {
            _cloudsShader = assetManager.GetShaderProgram("clouds_legacy");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Unable to load clouds_legacy shader. Ensure clouds_legacy.shader is compiled.",
                ex
            );
        }

        Order = 10;
        IgnoreFrustumCulling = true;
        CreateGeometry();
    }

    /// <summary>
    /// Adds a cloud instance.
    /// </summary>
    public void AddCloud(Cloud cloud)
    {
        _clouds.Add(cloud);
    }

    /// <summary>
    /// Removes a cloud instance.
    /// </summary>
    public void RemoveCloud(Cloud cloud)
    {
        _clouds.Remove(cloud);
    }

    /// <summary>
    /// Clears all cloud instances.
    /// </summary>
    public void ClearClouds()
    {
        _clouds.Clear();
    }

    /// <summary>
    /// Generates random cloud instances inside the specified ranges.
    /// </summary>
    public void GenerateRandomClouds(
        int count,
        Vector3D<float> minPosition,
        Vector3D<float> maxPosition,
        Vector3D<float> minSize,
        Vector3D<float> maxSize
    )
    {
        var random = Random.Shared;

        for (int i = 0; i < count; i++)
        {
            var position = new Vector3D<float>(
                float.Lerp(minPosition.X, maxPosition.X, random.NextSingle()),
                float.Lerp(minPosition.Y, maxPosition.Y, random.NextSingle()),
                float.Lerp(minPosition.Z, maxPosition.Z, random.NextSingle())
            );

            var size = new Vector3D<float>(
                float.Lerp(minSize.X, maxSize.X, random.NextSingle()),
                float.Lerp(minSize.Y, maxSize.Y, random.NextSingle()),
                float.Lerp(minSize.Z, maxSize.Z, random.NextSingle())
            );

            AddCloud(new Cloud(position, size));
        }

        _logger.Information("Generated {Count} random clouds.", count);
    }

    /// <inheritdoc />
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _time += gameTime.GetElapsedSeconds();
    }

    /// <inheritdoc />
    public override void Draw(ICamera3D camera, GameTime gameTime)
    {
        // Set up basic matrices and lighting that apply to all clouds
        var lightDir = LightDirection;

        if (lightDir != Vector3D<float>.Zero)
        {
            lightDir = Vector3D.Normalize(lightDir);
        }

        _cloudsShader.Uniforms["uView"].SetValueMat4(camera.View.ToSystem());
        _cloudsShader.Uniforms["uProjection"].SetValueMat4(camera.Projection.ToSystem());
        _cloudsShader.Uniforms["ambient"].SetValueVec3(AmbientLight.ToSystem());
        _cloudsShader.Uniforms["lightDirection"].SetValueVec3(lightDir.ToSystem());

        base.Draw(camera, gameTime);
    }

    /// <inheritdoc />
    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (_vertexBuffer == null || _vertexCount == 0 || _clouds.Count == 0)
        {
            yield break;
        }

        // Render each cloud instance
        foreach (var cloud in _clouds)
        {
            // Create transformation matrix for this cloud instance
            var scale = System.Numerics.Matrix4x4.CreateScale(
                new System.Numerics.Vector3(cloud.Size.X, cloud.Size.Y, cloud.Size.Z)
            );
            var translation = System.Numerics.Matrix4x4.CreateTranslation(
                new System.Numerics.Vector3(
                    cloud.Position.X + Transform.Position.X,
                    cloud.Position.Y + Transform.Position.Y,
                    cloud.Position.Z + Transform.Position.Z
                )
            );

            var worldMatrix = scale * translation;
            _cloudsShader.Uniforms["uWorld"].SetValueMat4(worldMatrix);

            // Yield draw command for this cloud
            yield return new RenderCommand(
                RenderCommandType.DrawArray,
                new DrawArrayPayload(
                    _cloudsShader,
                    _vertexBuffer,
                    _vertexCount,
                    PrimitiveType.Triangles
                )
            );
        }
    }

    private void CreateGeometry()
    {
        // Unit cube vertices with normals - expanded to 36 vertices (12 triangles)
        // Each vertex is per-triangle to avoid needing indices
        var vertices = new CloudsVertex[]
        {
            // Front face (Z+) - triangle 1
            new(new Vector3D<float>(-0.5f, -0.5f, 0.5f), new Vector3D<float>(0f, 0f, 1f)),
            new(new Vector3D<float>(-0.5f, 0.5f, 0.5f), new Vector3D<float>(0f, 0f, 1f)),
            new(new Vector3D<float>(0.5f, -0.5f, 0.5f), new Vector3D<float>(0f, 0f, 1f)),

            // Front face (Z+) - triangle 2
            new(new Vector3D<float>(0.5f, -0.5f, 0.5f), new Vector3D<float>(0f, 0f, 1f)),
            new(new Vector3D<float>(-0.5f, 0.5f, 0.5f), new Vector3D<float>(0f, 0f, 1f)),
            new(new Vector3D<float>(0.5f, 0.5f, 0.5f), new Vector3D<float>(0f, 0f, 1f)),

            // Back face (Z-) - triangle 1
            new(new Vector3D<float>(0.5f, -0.5f, -0.5f), new Vector3D<float>(0f, 0f, -1f)),
            new(new Vector3D<float>(0.5f, 0.5f, -0.5f), new Vector3D<float>(0f, 0f, -1f)),
            new(new Vector3D<float>(-0.5f, -0.5f, -0.5f), new Vector3D<float>(0f, 0f, -1f)),

            // Back face (Z-) - triangle 2
            new(new Vector3D<float>(-0.5f, -0.5f, -0.5f), new Vector3D<float>(0f, 0f, -1f)),
            new(new Vector3D<float>(0.5f, 0.5f, -0.5f), new Vector3D<float>(0f, 0f, -1f)),
            new(new Vector3D<float>(-0.5f, 0.5f, -0.5f), new Vector3D<float>(0f, 0f, -1f)),

            // Top face (Y+) - triangle 1
            new(new Vector3D<float>(-0.5f, 0.5f, 0.5f), new Vector3D<float>(0f, 1f, 0f)),
            new(new Vector3D<float>(-0.5f, 0.5f, -0.5f), new Vector3D<float>(0f, 1f, 0f)),
            new(new Vector3D<float>(0.5f, 0.5f, 0.5f), new Vector3D<float>(0f, 1f, 0f)),

            // Top face (Y+) - triangle 2
            new(new Vector3D<float>(0.5f, 0.5f, 0.5f), new Vector3D<float>(0f, 1f, 0f)),
            new(new Vector3D<float>(-0.5f, 0.5f, -0.5f), new Vector3D<float>(0f, 1f, 0f)),
            new(new Vector3D<float>(0.5f, 0.5f, -0.5f), new Vector3D<float>(0f, 1f, 0f)),

            // Bottom face (Y-) - triangle 1
            new(new Vector3D<float>(-0.5f, -0.5f, -0.5f), new Vector3D<float>(0f, -1f, 0f)),
            new(new Vector3D<float>(-0.5f, -0.5f, 0.5f), new Vector3D<float>(0f, -1f, 0f)),
            new(new Vector3D<float>(0.5f, -0.5f, -0.5f), new Vector3D<float>(0f, -1f, 0f)),

            // Bottom face (Y-) - triangle 2
            new(new Vector3D<float>(0.5f, -0.5f, -0.5f), new Vector3D<float>(0f, -1f, 0f)),
            new(new Vector3D<float>(-0.5f, -0.5f, 0.5f), new Vector3D<float>(0f, -1f, 0f)),
            new(new Vector3D<float>(0.5f, -0.5f, 0.5f), new Vector3D<float>(0f, -1f, 0f)),

            // Right face (X+) - triangle 1
            new(new Vector3D<float>(0.5f, -0.5f, 0.5f), new Vector3D<float>(1f, 0f, 0f)),
            new(new Vector3D<float>(0.5f, 0.5f, 0.5f), new Vector3D<float>(1f, 0f, 0f)),
            new(new Vector3D<float>(0.5f, -0.5f, -0.5f), new Vector3D<float>(1f, 0f, 0f)),

            // Right face (X+) - triangle 2
            new(new Vector3D<float>(0.5f, -0.5f, -0.5f), new Vector3D<float>(1f, 0f, 0f)),
            new(new Vector3D<float>(0.5f, 0.5f, 0.5f), new Vector3D<float>(1f, 0f, 0f)),
            new(new Vector3D<float>(0.5f, 0.5f, -0.5f), new Vector3D<float>(1f, 0f, 0f)),

            // Left face (X-) - triangle 1
            new(new Vector3D<float>(-0.5f, -0.5f, -0.5f), new Vector3D<float>(-1f, 0f, 0f)),
            new(new Vector3D<float>(-0.5f, -0.5f, 0.5f), new Vector3D<float>(-1f, 0f, 0f)),
            new(new Vector3D<float>(-0.5f, 0.5f, -0.5f), new Vector3D<float>(-1f, 0f, 0f)),

            // Left face (X-) - triangle 2
            new(new Vector3D<float>(-0.5f, 0.5f, -0.5f), new Vector3D<float>(-1f, 0f, 0f)),
            new(new Vector3D<float>(-0.5f, -0.5f, 0.5f), new Vector3D<float>(-1f, 0f, 0f)),
            new(new Vector3D<float>(-0.5f, 0.5f, 0.5f), new Vector3D<float>(-1f, 0f, 0f)),
        };

        _vertexBuffer?.Dispose();
        _vertexBuffer = new VertexBuffer<CloudsVertex>(GraphicsDevice, vertices, BufferUsage.StaticCopy);
        _vertexCount = (uint)vertices.Length;

        _logger.Information("Cloud geometry initialized (vertices: {VertexCount}).", vertices.Length);
    }
}
