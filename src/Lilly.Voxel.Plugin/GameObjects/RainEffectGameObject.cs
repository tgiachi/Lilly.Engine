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

public class RainEffectGameObject : Base3dGameObject
{
    private const int MaxDrops = 2048;

    private readonly ShaderProgram _rainShader;
    private readonly IAssetManager _assetManager;
    private readonly ILogger _logger = Log.ForContext<RainEffectGameObject>();
    private readonly Random _random = new();
    private readonly GraphicsDevice _graphicsDevice;

    private VertexBuffer<RainVertex>? _vertexBuffer;
    private readonly RainVertex[] _vertices = new RainVertex[MaxDrops * 6]; // 6 vertices per quad (2 triangles)
    private readonly RainDrop[] _drops = new RainDrop[MaxDrops];

    // Configuration
    public int MaxDropsCount => MaxDrops;
    public int ActiveDropCount { get; private set; }
    public Vector3 AreaSize { get; set; } = new Vector3(200f, 250f, 200f);
    public float RainIntensity { get; set; } = 0.8f;
    public float MinSpeed { get; set; } = 20f;
    public float MaxSpeed { get; set; } = 35f;
    public float MinLength { get; set; } = 8f;
    public float MaxLength { get; set; } = 16f;
    public float DropWidth { get; set; } = 0.15f;
    public Vector3 RainDirection { get; set; } = new Vector3(0f, -1f, 0f);
    public Vector3 WindDirection { get; set; } = new Vector3(0.2f, 0f, 0.08f);
    public float RefractionStrength { get; set; } = 0.25f;
    public float DepthThreshold { get; set; } = 0.02f;

    // Resources
    // rain.shader doesn't require textures in this version

    // Internal state
    private float _animationTime;
    private Vector3 _lastCameraPosition;
    private bool _cameraPositionInitialized;

    public RainEffectGameObject(
        GraphicsDevice graphicsDevice,
        IAssetManager assetManager,
        IGameObjectManager gameObjectManager
    ) : base("RainEffect", gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
        _rainShader = assetManager.GetShaderProgram("rain"); // Maps to rain.shader
        ZIndex = 11;
        IgnoreFrustumCulling = true;
    }

    public override void Initialize()
    {
        InitBuffers();
        UpdateDropDensity();
        _logger.Information("RainEffectGameObject initialized with {DropCount} active drops.", ActiveDropCount);
    }

    private void InitBuffers()
    {
        // Create vertex buffer (will be recreated every frame with updated positions)
        _vertexBuffer = new VertexBuffer<RainVertex>(
            _graphicsDevice,
            _vertices,
            BufferUsage.StaticCopy
        );
    }

    public override void Update(GameTime gameTime)
    {
        var delta = gameTime.GetElapsedSeconds();
        _animationTime += delta;

        if (ActiveDropCount == 0)
        {
            base.Update(gameTime);
            return;
        }

        // Update all active drops
        for (int i = 0; i < ActiveDropCount; i++)
        {
            ref var drop = ref _drops[i];

            // Rain falls straight down
            var rainMovement = RainDirection * drop.Velocity * delta;

            // Wind sway per-drop (oscillating)
            float windStrength = MathF.Sin(_animationTime * 1.5f + drop.Position.Z * 0.3f) * 0.2f;
            float windStrengthZ = MathF.Cos(_animationTime * 1.2f + drop.Position.X * 0.3f) * 0.15f;
            var windOffset = new Vector3(
                                 WindDirection.X * windStrength,
                                 WindDirection.Y * windStrength * 0.3f,
                                 WindDirection.Z * windStrengthZ
                             ) *
                             delta;

            // Update position: rain direction + wind sway
            drop.Position += rainMovement + windOffset;

            // Respawn if fell below ground
            if (drop.Position.Y < 0f)
            {
                RespawnDrop(ref drop);
            }
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera)
    {
        if (ActiveDropCount == 0 || _vertexBuffer == null)
        {
            return;
        }

        // Compensate for camera movement to keep drops at fixed world positions
        if (_cameraPositionInitialized)
        {
            var cameraDelta = camera.Position - _lastCameraPosition;

            for (int i = 0; i < ActiveDropCount; i++)
            {
                _drops[i].Position -= cameraDelta;
            }
        }
        _lastCameraPosition = camera.Position;
        _cameraPositionInitialized = true;

        // Center rain volume on the camera height so drops fall around the viewer instead of only above
        Transform.Position = camera.Position - new Vector3(0, AreaSize.Y * 0.5f, 0);

        // Set up matrices
        _rainShader.Uniforms["uWorld"].SetValueMat4(Transform.GetTransformationMatrix());
        _rainShader.Uniforms["uView"].SetValueMat4(camera.View);
        _rainShader.Uniforms["uProjection"].SetValueMat4(camera.Projection);

        // Set camera and effect parameters
        var cameraRight = Vector3.Normalize(
             new Vector3(
                 camera.View.M11,
                 camera.View.M21,
                 camera.View.M31
             )
         );

        _rainShader.Uniforms["uCameraRight"].SetValueVec3(cameraRight);
        _rainShader.Uniforms["uRainDirection"].SetValueVec3(Vector3.Normalize(RainDirection));
        _rainShader.Uniforms["uDropWidth"].SetValueFloat(DropWidth);
        _rainShader.Uniforms["uIntensity"].SetValueFloat(RainIntensity);
        _rainShader.Uniforms["uTime"].SetValueFloat(_animationTime);

        // Update vertex data with current drop positions
        FillVertexData();

        // Recreate vertex buffer with updated data
        _vertexBuffer?.Dispose();
        _vertexBuffer = new VertexBuffer<RainVertex>(
            graphicsDevice,
            _vertices,
            BufferUsage.StaticCopy
        );

        // Depth state setting
        graphicsDevice.DepthState = new DepthState(false); // No depth test, no depth write

        // Drawing
        graphicsDevice.ShaderProgram = _rainShader;
        graphicsDevice.VertexArray = _vertexBuffer;
        graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, (uint)(ActiveDropCount * 6));

        // Restore default depth state
        graphicsDevice.DepthState = DepthState.Default;
    }

    private void UpdateDropDensity()
    {
        var target = (int)MathF.Round(RainIntensity * MaxDrops);
        target = Math.Clamp(target, 0, MaxDrops);

        if (target > ActiveDropCount)
        {
            // Spawn new drops
            for (int i = ActiveDropCount; i < target; i++)
            {
                InitializeDrop(i, randomHeight: true);
            }
        }

        ActiveDropCount = target;
    }

    private void InitializeDrop(int index, bool randomHeight)
    {
        var halfWidth = AreaSize.X * 0.5f;
        var halfDepth = AreaSize.Z * 0.5f;

        var position = new Vector3(
            Lerp(-halfWidth, halfWidth, (float)_random.NextDouble()),
            randomHeight ? Lerp(0f, AreaSize.Y, (float)_random.NextDouble()) : AreaSize.Y,
            Lerp(-halfDepth, halfDepth, (float)_random.NextDouble())
        );

        var velocity = Lerp(MinSpeed, MaxSpeed, (float)_random.NextDouble());
        var length = Lerp(MinLength, MaxLength, (float)_random.NextDouble());
        var alpha = Lerp(0.55f, 1f, (float)_random.NextDouble());

        _drops[index] = new RainDrop
        {
            Position = position,
            Velocity = velocity,
            Length = length,
            Alpha = alpha
        };
    }

    private void RespawnDrop(ref RainDrop drop)
    {
        var halfWidth = AreaSize.X * 0.5f;
        var halfDepth = AreaSize.Z * 0.5f;

        drop.Position = new Vector3(
            Lerp(-halfWidth, halfWidth, (float)_random.NextDouble()),
            AreaSize.Y + Lerp(0f, AreaSize.Y * 0.2f, (float)_random.NextDouble()),
            Lerp(-halfDepth, halfDepth, (float)_random.NextDouble())
        );
        drop.Velocity = Lerp(MinSpeed, MaxSpeed, (float)_random.NextDouble());
        drop.Length = Lerp(MinLength, MaxLength, (float)_random.NextDouble());
        drop.Alpha = Lerp(0.55f, 1f, (float)_random.NextDouble());
    }

    private void FillVertexData()
    {
        for (int i = 0; i < ActiveDropCount; i++)
        {
            var drop = _drops[i];
            var vertexIndex = i * 6;

            // Triangle 1: (0,0), (1,0), (0,1)
            _vertices[vertexIndex + 0] = new RainVertex(
                drop.Position,
                new Vector2(0f, 0f),
                drop.Length,
                drop.Alpha
            );

            _vertices[vertexIndex + 1] = new RainVertex(
                drop.Position,
                new Vector2(1f, 0f),
                drop.Length,
                drop.Alpha
            );

            _vertices[vertexIndex + 2] = new RainVertex(
                drop.Position,
                new Vector2(0f, 1f),
                drop.Length,
                drop.Alpha
            );

            // Triangle 2: (0,1), (1,0), (1,1)
            _vertices[vertexIndex + 3] = new RainVertex(
                drop.Position,
                new Vector2(0f, 1f),
                drop.Length,
                drop.Alpha
            );

            _vertices[vertexIndex + 4] = new RainVertex(
                drop.Position,
                new Vector2(1f, 0f),
                drop.Length,
                drop.Alpha
            );

            _vertices[vertexIndex + 5] = new RainVertex(
                drop.Position,
                new Vector2(1f, 1f),
                drop.Length,
                drop.Alpha
            );
        }
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    private struct RainDrop
    {
        public Vector3 Position;
        public float Velocity;
        public float Length;
        public float Alpha;
    }
}
