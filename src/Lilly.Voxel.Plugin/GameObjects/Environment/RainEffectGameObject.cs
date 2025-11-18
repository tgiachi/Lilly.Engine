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

public class RainEffectGameObject : BaseGameObject3D
{
    private const int MaxDrops = 2048;

    private readonly ShaderProgram _rainShader;
    private readonly IAssetManager _assetManager;
    private readonly ILogger _logger = Log.ForContext<RainEffectGameObject>();
    private readonly Random _random = new();

    private VertexBuffer<RainVertex>? _vertexBuffer;
    private readonly RainVertex[] _vertices = new RainVertex[MaxDrops * 6]; // 6 vertices per quad (2 triangles)
    private readonly RainDrop[] _drops = new RainDrop[MaxDrops];

    // Configuration
    public int MaxDropsCount => MaxDrops;
    public int ActiveDropCount { get; private set; }
    public Vector3D<float> AreaSize { get; set; } = new Vector3D<float>(200f, 250f, 200f);
    public float RainIntensity { get; set; } = 0.8f;
    public float MinSpeed { get; set; } = 20f;
    public float MaxSpeed { get; set; } = 35f;
    public float MinLength { get; set; } = 8f;
    public float MaxLength { get; set; } = 16f;
    public float DropWidth { get; set; } = 0.15f;
    public Vector3D<float> RainDirection { get; set; } = new Vector3D<float>(0f, -1f, 0f);
    public Vector3D<float> WindDirection { get; set; } = new Vector3D<float>(0.2f, 0f, 0.08f);
    public float RefractionStrength { get; set; } = 0.25f;
    public float DepthThreshold { get; set; } = 0.02f;

    // Resources
    public Texture2D RainTexture { get; set; }
    public Texture2D NormalTexture { get; set; }
    public Texture2D DepthTexture { get; set; }

    // Internal state
    private float _animationTime;
    private Vector3D<float> _lastCameraPosition;
    private bool _cameraPositionInitialized;

    public RainEffectGameObject(GraphicsDevice graphicsDevice, IAssetManager assetManager) : base(graphicsDevice)
    {
        _assetManager = assetManager;
        _rainShader = assetManager.GetShaderProgram("rain_legacy");
        Order = 11;
        IgnoreFrustumCulling = true;

        // rain_legacy.shader doesn't require textures
    }

    public override void Initialize()
    {
        InitBuffers();
        UpdateDropDensity();
        _logger.Information("RainEffectGameObject initialized with {DropCount} active drops.", ActiveDropCount);
        base.Initialize();
    }

    private void InitBuffers()
    {
        // Create vertex buffer (will be recreated every frame with updated positions)
        _vertexBuffer = new VertexBuffer<RainVertex>(
            GraphicsDevice,
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
            var windOffset = new Vector3D<float>(
                WindDirection.X * windStrength,
                WindDirection.Y * windStrength * 0.3f,
                WindDirection.Z * windStrengthZ
            ) * delta;

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

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (ActiveDropCount == 0 || _vertexBuffer == null)
        {
            yield break;
        }

        // Update vertex data with current drop positions
        FillVertexData();

        // Recreate vertex buffer with updated data
        _vertexBuffer?.Dispose();
        _vertexBuffer = new VertexBuffer<RainVertex>(
            GraphicsDevice,
            _vertices,
            BufferUsage.StaticCopy
        );

        yield return RenderCommandHelpers.SetDepthState(
            new SetDepthStatePayload(
                depthTestEnabled: false,
                depthWriteEnabled: false
            )
        );

        yield return new RenderCommand(
            RenderCommandType.DrawArray,
            new DrawArrayPayload(
                _rainShader,
                _vertexBuffer,
                (uint)(ActiveDropCount * 6), // 6 vertices per quad (2 triangles)
                PrimitiveType.Triangles
            )
        );

        yield return RenderCommandHelpers.SetDepthState(SetDepthStatePayload.DefaultDepthState());
    }

    public override void Draw(ICamera3D camera, GameTime gameTime)
    {
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
        Transform.Position = camera.Position - new Vector3D<float>(0, AreaSize.Y * 0.5f, 0);

        // Set up matrices
        _rainShader.Uniforms["uWorld"].SetValueMat4(Transform.GetTransformationMatrix().ToSystem());
        _rainShader.Uniforms["uView"].SetValueMat4(camera.View.ToSystem());
        _rainShader.Uniforms["uProjection"].SetValueMat4(camera.Projection.ToSystem());

        // Set camera and effect parameters
        _rainShader.Uniforms["uCameraRight"].SetValueVec3(Vector3D.Normalize(camera.Right).ToSystem());
        _rainShader.Uniforms["uRainDirection"].SetValueVec3(Vector3D.Normalize(RainDirection).ToSystem());
        _rainShader.Uniforms["uDropWidth"].SetValueFloat(DropWidth);
        _rainShader.Uniforms["uIntensity"].SetValueFloat(RainIntensity);
        _rainShader.Uniforms["uTime"].SetValueFloat(_animationTime);

        base.Draw(camera, gameTime);
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

        var position = new Vector3D<float>(
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

        drop.Position = new Vector3D<float>(
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
                new Vector2D<float>(0f, 0f),
                drop.Length,
                drop.Alpha
            );

            _vertices[vertexIndex + 1] = new RainVertex(
                drop.Position,
                new Vector2D<float>(1f, 0f),
                drop.Length,
                drop.Alpha
            );

            _vertices[vertexIndex + 2] = new RainVertex(
                drop.Position,
                new Vector2D<float>(0f, 1f),
                drop.Length,
                drop.Alpha
            );

            // Triangle 2: (0,1), (1,0), (1,1)
            _vertices[vertexIndex + 3] = new RainVertex(
                drop.Position,
                new Vector2D<float>(0f, 1f),
                drop.Length,
                drop.Alpha
            );

            _vertices[vertexIndex + 4] = new RainVertex(
                drop.Position,
                new Vector2D<float>(1f, 0f),
                drop.Length,
                drop.Alpha
            );

            _vertices[vertexIndex + 5] = new RainVertex(
                drop.Position,
                new Vector2D<float>(1f, 1f),
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
        public Vector3D<float> Position;
        public float Velocity;
        public float Length;
        public float Alpha;
    }
}
