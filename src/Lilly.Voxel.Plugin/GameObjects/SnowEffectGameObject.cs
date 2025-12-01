using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Voxel.Plugin.Vertexs;
using Serilog;
using System.Numerics;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

public class SnowEffectGameObject : Base3dGameObject
{
    private const int MaxFlakes = 4096;

    private readonly ShaderProgram _snowShader;
    private readonly IAssetManager _assetManager;
    private readonly ILogger _logger = Log.ForContext<SnowEffectGameObject>();
    private readonly Random _random = new();
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IGameObjectManager _gameObjectManager;

    private VertexBuffer<SnowVertex>? _vertexBuffer;
    private readonly SnowVertex[] _vertices = new SnowVertex[MaxFlakes * 6]; // 6 vertices per quad (2 triangles)
    private readonly SnowFlake[] _flakes = new SnowFlake[MaxFlakes];

    // Configuration
    public int ActiveFlakeCount { get; private set; }
    public Vector3 AreaSize { get; set; } = new Vector3(140f, 90f, 140f);
    public float SnowIntensity { get; set; } = 0.5f;
    public float MinSpeed { get; set; } = 3f;
    public float MaxSpeed { get; set; } = 9f;
    public float MinSize { get; set; } = 0.15f;
    public float MaxSize { get; set; } = 0.4f;
    public Vector3 WindDirection { get; set; } = new Vector3(0.4f, 0f, 0.2f);
    public float DepthThreshold { get; set; } = 0.05f;
    public float ParticleRotation { get; set; }

    // Resources
    public Texture2D SnowflakeTexture { get; set; }
    public Texture2D NoiseTexture { get; set; }
    public Texture2D DepthTexture { get; set; }

    // Internal state
    private float _animationTime;
    private Vector3 _lastCameraPosition;
    private bool _cameraPositionInitialized;

    public SnowEffectGameObject(
        GraphicsDevice graphicsDevice,
        IAssetManager assetManager,
        IGameObjectManager gameObjectManager
    ) : base("SnowEffect", gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
        _snowShader = assetManager.GetShaderProgram("snow");
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
    }

    private void InitBuffers()
    {
        // Create vertex buffer (will be recreated every frame with updated positions)
        _vertexBuffer = new VertexBuffer<SnowVertex>(
            _graphicsDevice,
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
            var sway = new Vector3(flutter * 0.6f, 0f, flutter * 0.4f) * delta;

            // Update position: gravity + wind + sway
            flake.Position += flake.Velocity * delta + drift + sway;

            // Wrap vertical position (Falling)
            if (flake.Position.Y < 0f)
            {
                flake.Position.Y += AreaSize.Y;
            }
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera)
    {
                // Compensate for camera movement to keep flakes at fixed world positions
                if (_cameraPositionInitialized)
                {
                    var cameraDelta = camera.Position - _lastCameraPosition;
                    var halfSize = AreaSize * 0.5f;

                    for (int i = 0; i < ActiveFlakeCount; i++)
                    {
                        ref var flakePos = ref _flakes[i].Position;
                        flakePos -= cameraDelta;

                        // Infinite Wrapping Logic (Toroidal space)

                        // Wrap X
                        if (flakePos.X < -halfSize.X) flakePos.X += AreaSize.X;
                        else if (flakePos.X > halfSize.X) flakePos.X -= AreaSize.X;

                        // Wrap Z
                        if (flakePos.Z < -halfSize.Z) flakePos.Z += AreaSize.Z;
                        else if (flakePos.Z > halfSize.Z) flakePos.Z -= AreaSize.Z;

                        // Wrap Y (Optional, helps if camera moves up/down fast)
                        if (flakePos.Y < 0) flakePos.Y += AreaSize.Y;
                        else if (flakePos.Y > AreaSize.Y) flakePos.Y -= AreaSize.Y;
                    }
                }
                _lastCameraPosition = camera.Position;
                _cameraPositionInitialized = true;

        // Center snow volume on the camera so flakes fall around the viewer in all directions
        Transform.Position = camera.Position - new Vector3(0, AreaSize.Y * 0.5f, 0);

        // Set up matrices
        _snowShader.Uniforms["uWorld"].SetValueMat4(Transform.GetTransformationMatrix());
        _snowShader.Uniforms["uView"].SetValueMat4(camera.View);
        _snowShader.Uniforms["uProjection"].SetValueMat4(camera.Projection);

        // Set camera-relative vectors for billboard
        var cameraRight = Vector3.Normalize(
            new Vector3(
                camera.View.M11,
                camera.View.M21,
                camera.View.M31
            )
        );
        var cameraUp = Vector3.Normalize(
            new Vector3(
                camera.View.M12,
                camera.View.M22,
                camera.View.M32
            )
        );

        _snowShader.Uniforms["uCameraRight"].SetValueVec3(cameraRight);
        _snowShader.Uniforms["uCameraUp"].SetValueVec3(cameraUp);

        // Set effect parameters
        _snowShader.Uniforms["uIntensity"].SetValueFloat(SnowIntensity);
        _snowShader.Uniforms["uTime"].SetValueFloat(_animationTime);

        //_snowShader.Uniforms["uDepthThreshold"].SetValueFloat(DepthThreshold);
        //_snowShader.Uniforms["uWindDirection"].SetValueVec3(WindDirection);
        //_snowShader.Uniforms["uParticleRotation"].SetValueFloat(ParticleRotation);

        // Set textures
        //_snowShader.Uniforms["uSnowflakeTexture"]
        //           .SetValueTexture(SnowflakeTexture ?? _assetManager.GetWhiteTexture<Texture2D>());
        //_snowShader.Uniforms["uDepthTexture"].SetValueTexture(DepthTexture ?? _assetManager.GetWhiteTexture<Texture2D>());

        if (ActiveFlakeCount == 0 || _vertexBuffer == null)
        {
            return;
        }

        // Update vertex data with current flake positions
        FillVertexData();

        // Recreate vertex buffer with updated data
        _vertexBuffer?.Dispose();
        _vertexBuffer = new VertexBuffer<SnowVertex>(
            graphicsDevice,
            _vertices,
            BufferUsage.StaticCopy
        );

        // Depth state setting
        graphicsDevice.DepthState = new DepthState(true);

        // Drawing
        graphicsDevice.ShaderProgram = _snowShader;
        graphicsDevice.VertexArray = _vertexBuffer;
        graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, (uint)(ActiveFlakeCount * 6));

        // Restore default depth state
        graphicsDevice.DepthState = DepthState.Default;
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

        var position = new Vector3(
            Lerp(-halfWidth, halfWidth, (float)_random.NextDouble()),
            randomHeight ? Lerp(0f, AreaSize.Y, (float)_random.NextDouble()) : AreaSize.Y,
            Lerp(-halfDepth, halfDepth, (float)_random.NextDouble())
        );

        var velocity = new Vector3(0f, -Lerp(MinSpeed, MaxSpeed, (float)_random.NextDouble()), 0f);
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

        flake.Position = new Vector3(
            Lerp(-halfWidth, halfWidth, (float)_random.NextDouble()),
            AreaSize.Y + Lerp(0f, AreaSize.Y * 0.2f, (float)_random.NextDouble()),
            Lerp(-halfDepth, halfDepth, (float)_random.NextDouble())
        );
        flake.Velocity = new Vector3(0f, -Lerp(MinSpeed, MaxSpeed, (float)_random.NextDouble()), 0f);
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
                new Vector2(0f, 0f),
                flake.Size,
                flake.Alpha
            );

            _vertices[vertexIndex + 1] = new SnowVertex(
                flake.Position,
                new Vector2(1f, 0f),
                flake.Size,
                flake.Alpha
            );

            _vertices[vertexIndex + 2] = new SnowVertex(
                flake.Position,
                new Vector2(0f, 1f),
                flake.Size,
                flake.Alpha
            );

            // Triangle 2: (0,1), (1,0), (1,1)
            _vertices[vertexIndex + 3] = new SnowVertex(
                flake.Position,
                new Vector2(0f, 1f),
                flake.Size,
                flake.Alpha
            );

            _vertices[vertexIndex + 4] = new SnowVertex(
                flake.Position,
                new Vector2(1f, 0f),
                flake.Size,
                flake.Alpha
            );

            _vertices[vertexIndex + 5] = new SnowVertex(
                flake.Position,
                new Vector2(1f, 1f),
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
        public Vector3 Position;
        public Vector3 Velocity;
        public float Size;
        public float Alpha;
    }
}
