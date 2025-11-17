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

namespace Lilly.Voxel.Plugin.GameObjects.Environment;

public class SnowEffectGameObject : BaseGameObject3D
{
    private const int MaxFlakes = 2048;

    private readonly ShaderProgram _snowShader;
    private readonly IAssetManager _assetManager;
    private readonly ILogger _logger = Log.ForContext<SnowEffectGameObject>();
    private readonly Random _random = new();

    private VertexBuffer<SnowVertex>? _vertexBuffer;
    private readonly SnowVertex[] _vertices = new SnowVertex[MaxFlakes * 6]; // 6 vertices per quad (2 triangles)
    private readonly SnowFlake[] _flakes = new SnowFlake[MaxFlakes];

    // Configuration
    public int MaxFlakesCount => MaxFlakes;
    public int ActiveFlakeCount { get; private set; }
    public Vector3D<float> AreaSize { get; set; } = new Vector3D<float>(140f, 90f, 140f);
    public float SnowIntensity { get; set; } = 0.5f;
    public float MinSpeed { get; set; } = 3f;
    public float MaxSpeed { get; set; } = 9f;
    public float MinSize { get; set; } = 0.35f;
    public float MaxSize { get; set; } = 0.9f;
    public Vector3D<float> WindDirection { get; set; } = new Vector3D<float>(0.4f, 0f, 0.2f);
    public float DepthThreshold { get; set; } = 0.05f;
    public float ParticleRotation { get; set; }

    // Resources
    public Texture2D SnowflakeTexture { get; set; }
    public Texture2D NoiseTexture { get; set; }
    public Texture2D DepthTexture { get; set; }

    // Internal state
    private float _animationTime;
    private Vector3D<float> _lastCameraPosition;
    private bool _cameraPositionInitialized = false;

    public SnowEffectGameObject(GraphicsDevice graphicsDevice, IAssetManager assetManager) : base(graphicsDevice)
    {
        _assetManager = assetManager;
        _snowShader = assetManager.GetShaderProgram("snow");
        Order = 10;
        IsVisible = true;
        IgnoreFrustumCulling = true;

        // Try to load textures
        try
        {
            SnowflakeTexture = assetManager.GetTexture<Texture2D>("snowflake");
        }
        catch
        {
            SnowflakeTexture = assetManager.GetWhiteTexture<Texture2D>();
        }

        DepthTexture = assetManager.GetWhiteTexture<Texture2D>();
    }

    public override void Initialize()
    {
        InitBuffers();
        UpdateFlakeDensity();
        _logger.Information("SnowEffectGameObject initialized with {FlakeCount} active flakes.", ActiveFlakeCount);
        base.Initialize();
    }

    private void InitBuffers()
    {
        // Create vertex buffer (will be recreated every frame with updated positions)
        _vertexBuffer = new VertexBuffer<SnowVertex>(
            GraphicsDevice,
            _vertices,
            BufferUsage.StaticCopy
        );
    }

    public override void Update(GameTime gameTime)
    {
        var delta = gameTime.GetElapsedSeconds();
        _animationTime += delta;

        if (ActiveFlakeCount == 0)
        {
            base.Update(gameTime);

            return;
        }

        // Update all active flakes
        for (int i = 0; i < ActiveFlakeCount; i++)
        {
            ref var flake = ref _flakes[i];

            // Apply wind drift
            var drift = WindDirection * delta;

            // Apply flutter (sine wave based on time and position)
            float flutter = MathF.Sin(_animationTime * 1.1f + flake.Position.X * 0.3f + flake.Position.Z * 0.2f);
            var sway = new Vector3D<float>(flutter * 0.6f, 0f, flutter * 0.4f) * delta;

            // Update position: gravity + wind + sway
            flake.Position += flake.Velocity * delta + drift + sway;

            // Respawn if fell below ground
            if (flake.Position.Y < 0f)
            {
                RespawnFlake(ref flake);
            }
        }

        base.Update(gameTime);
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (ActiveFlakeCount == 0 || _vertexBuffer == null)
        {
            yield break;
        }

        // Update vertex data with current flake positions
        FillVertexData();

        // Recreate vertex buffer with updated data
        _vertexBuffer?.Dispose();
        _vertexBuffer = new VertexBuffer<SnowVertex>(
            GraphicsDevice,
            _vertices,
            BufferUsage.StaticCopy
        );

        yield return RenderCommandHelpers.SetDepthState(
            new SetDepthStatePayload(
                depthTestEnabled: true,
                depthWriteEnabled: false
            )
        );

        yield return new RenderCommand(
            RenderCommandType.DrawArray,
            new DrawArrayPayload(
                _snowShader,
                _vertexBuffer,
                (uint)(ActiveFlakeCount * 6), // 6 vertices per quad (2 triangles)
                PrimitiveType.Triangles
            )
        );

        yield return RenderCommandHelpers.SetDepthState(SetDepthStatePayload.DefaultDepthState());
    }

    public override void Draw(ICamera3D camera, GameTime gameTime)
    {
        // Compensate for camera movement to keep flakes at fixed world positions
        if (_cameraPositionInitialized)
        {
            var cameraDelta = camera.Position - _lastCameraPosition;
            for (int i = 0; i < ActiveFlakeCount; i++)
            {
                _flakes[i].Position -= cameraDelta;
            }
        }
        _lastCameraPosition = camera.Position;
        _cameraPositionInitialized = true;

        // Center snow volume on the camera so flakes fall around the viewer in all directions
        Transform.Position = camera.Position - new Vector3D<float>(0, AreaSize.Y * 0.5f, 0);

        // Set up matrices
        _snowShader.Uniforms["uWorld"].SetValueMat4(Transform.GetTransformationMatrix().ToSystem());
        _snowShader.Uniforms["uView"].SetValueMat4(camera.View.ToSystem());
        _snowShader.Uniforms["uProjection"].SetValueMat4(camera.Projection.ToSystem());

        // Set camera-relative vectors for billboard
        var cameraRight = Vector3D.Normalize(
            new Vector3D<float>(
                camera.View.M11,
                camera.View.M21,
                camera.View.M31
            )
        );
        var cameraUp = Vector3D.Normalize(
            new Vector3D<float>(
                camera.View.M12,
                camera.View.M22,
                camera.View.M32
            )
        );

        _snowShader.Uniforms["uCameraRight"].SetValueVec3(cameraRight.ToSystem());
        _snowShader.Uniforms["uCameraUp"].SetValueVec3(cameraUp.ToSystem());

        // Set effect parameters
        _snowShader.Uniforms["uIntensity"].SetValueFloat(SnowIntensity);
        _snowShader.Uniforms["uTime"].SetValueFloat(_animationTime);
        _snowShader.Uniforms["uDepthThreshold"].SetValueFloat(DepthThreshold);
        _snowShader.Uniforms["uWindDirection"].SetValueVec3(WindDirection.ToSystem());
        _snowShader.Uniforms["uParticleRotation"].SetValueFloat(ParticleRotation);

        // Set textures
        _snowShader.Uniforms["uSnowflakeTexture"]
                   .SetValueTexture(SnowflakeTexture ?? _assetManager.GetWhiteTexture<Texture2D>());
        _snowShader.Uniforms["uDepthTexture"].SetValueTexture(DepthTexture ?? _assetManager.GetWhiteTexture<Texture2D>());

        base.Draw(camera, gameTime);
    }

    private void UpdateFlakeDensity()
    {
        var target = (int)MathF.Round(SnowIntensity * MaxFlakes);
        target = Math.Clamp(target, 0, MaxFlakes);

        if (target > ActiveFlakeCount)
        {
            // Spawn new flakes
            for (int i = ActiveFlakeCount; i < target; i++)
            {
                InitializeFlake(i, randomHeight: true);
            }
        }

        ActiveFlakeCount = target;
    }

    private void InitializeFlake(int index, bool randomHeight)
    {
        var halfWidth = AreaSize.X * 0.5f;
        var halfDepth = AreaSize.Z * 0.5f;

        var position = new Vector3D<float>(
            Lerp(-halfWidth, halfWidth, (float)_random.NextDouble()),
            randomHeight ? Lerp(0f, AreaSize.Y, (float)_random.NextDouble()) : AreaSize.Y,
            Lerp(-halfDepth, halfDepth, (float)_random.NextDouble())
        );

        var velocity = new Vector3D<float>(0f, -Lerp(MinSpeed, MaxSpeed, (float)_random.NextDouble()), 0f);
        var size = Lerp(MinSize, MaxSize, (float)_random.NextDouble());
        var alpha = Lerp(0.55f, 1f, (float)_random.NextDouble());

        _flakes[index] = new SnowFlake
        {
            Position = position,
            Velocity = velocity,
            Size = size,
            Alpha = alpha
        };
    }

    private void RespawnFlake(ref SnowFlake flake)
    {
        var halfWidth = AreaSize.X * 0.5f;
        var halfDepth = AreaSize.Z * 0.5f;

        flake.Position = new Vector3D<float>(
            Lerp(-halfWidth, halfWidth, (float)_random.NextDouble()),
            AreaSize.Y + Lerp(0f, AreaSize.Y * 0.2f, (float)_random.NextDouble()),
            Lerp(-halfDepth, halfDepth, (float)_random.NextDouble())
        );
        flake.Velocity = new Vector3D<float>(0f, -Lerp(MinSpeed, MaxSpeed, (float)_random.NextDouble()), 0f);
        flake.Size = Lerp(MinSize, MaxSize, (float)_random.NextDouble());
        flake.Alpha = Lerp(0.55f, 1f, (float)_random.NextDouble());
    }

    private void FillVertexData()
    {
        for (int i = 0; i < ActiveFlakeCount; i++)
        {
            var flake = _flakes[i];
            var vertexIndex = i * 6;

            // Triangle 1: (0,0), (1,0), (0,1)
            _vertices[vertexIndex + 0] = new SnowVertex(
                flake.Position,
                new Vector2D<float>(0f, 0f),
                flake.Size,
                flake.Alpha
            );

            _vertices[vertexIndex + 1] = new SnowVertex(
                flake.Position,
                new Vector2D<float>(1f, 0f),
                flake.Size,
                flake.Alpha
            );

            _vertices[vertexIndex + 2] = new SnowVertex(
                flake.Position,
                new Vector2D<float>(0f, 1f),
                flake.Size,
                flake.Alpha
            );

            // Triangle 2: (0,1), (1,0), (1,1)
            _vertices[vertexIndex + 3] = new SnowVertex(
                flake.Position,
                new Vector2D<float>(0f, 1f),
                flake.Size,
                flake.Alpha
            );

            _vertices[vertexIndex + 4] = new SnowVertex(
                flake.Position,
                new Vector2D<float>(1f, 0f),
                flake.Size,
                flake.Alpha
            );

            _vertices[vertexIndex + 5] = new SnowVertex(
                flake.Position,
                new Vector2D<float>(1f, 1f),
                flake.Size,
                flake.Alpha
            );
        }
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    private struct SnowFlake
    {
        public Vector3D<float> Position;
        public Vector3D<float> Velocity;
        public float Size;
        public float Alpha;
    }
}
