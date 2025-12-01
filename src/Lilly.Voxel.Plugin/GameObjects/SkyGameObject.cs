using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Vertexts;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Services;
using Serilog;
using System.Numerics;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

public class SkyGameObject : Base3dGameObject
{
    private readonly ShaderProgram _skyShader;
    private readonly IAssetManager _assetManager;
    private VertexBuffer<PositionVertex>? _vertexBuffer;
    private float _timeOfDay;
    public Texture2D? SkyTexture { get; set; }
    public Vector3 SunDirection { get; set; } = new Vector3(0.5f, 0.7f, 0.3f);
    public Vector3 MoonDirection { get; set; } = new Vector3(-0.5f, -0.7f, -0.3f);

    public bool UseTexture { get; set; }
    public bool EnableAurore { get; set; } = true;
    public float AuroreIntensity { get; set; } = 0.7f;
    public bool EnableCycle { get; set; } = true;
    public float CycleSpeed { get; set; } = 0.01f;

    public float TimeOfDay
    {
        get => _timeOfDay;
        set => _timeOfDay = value % 1.0f;
    }

    private readonly GraphicsDevice _graphicsDevice;

    public SkyGameObject(
        GraphicsDevice graphicsDevice,
        IAssetManager assetManager,
        IGameObjectManager gameObjectManager
    ) : base("Sky", gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        IgnoreFrustumCulling = true;
        _assetManager = assetManager;
        _skyShader = assetManager.GetShaderProgram("sky");
        ZIndex = 0;
        IsActive = true; // Use IsActive instead of IsVisible based on Base3dGameObject
    }

    public override void Initialize()
    {
        CreateSkyGeometry();
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }

        if (EnableCycle)
        {
            _timeOfDay += gameTime.GetElapsedSeconds() * CycleSpeed;
            _timeOfDay %= 1.0f;
        }

        UpdateLighting();

        base.Update(gameTime);
    }

    private void UpdateLighting()
    {
        const float TwoPi = MathF.PI * 2.0f;
        float sunAngle = _timeOfDay * TwoPi;
        float sunHeight = MathF.Sin(sunAngle);

        SunDirection = new Vector3(
            MathF.Cos(sunAngle),
            sunHeight,
            MathF.Sin(sunAngle) * 0.3f
        );
        SunDirection = Vector3.Normalize(SunDirection);

        float moonAngle = sunAngle + MathF.PI;
        float moonHeight = MathF.Sin(moonAngle);
        MoonDirection = new Vector3(
            MathF.Cos(moonAngle),
            moonHeight,
            MathF.Sin(moonAngle) * 0.3f
        );
        MoonDirection = Vector3.Normalize(MoonDirection);
    }

    public override void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera)
    {
        if (!IsActive || _vertexBuffer == null)
        {
            return;
        }

        const float skyboxSize = 500f;
        Transform.Position = camera.Position;
        Transform.Scale = new Vector3(skyboxSize, skyboxSize, skyboxSize);

        graphicsDevice.DepthState = new DepthState(true, DepthFunction.LessOrEqual); // Skybox depth state
        graphicsDevice.FaceCullingEnabled = false;
        _skyShader.Uniforms["uSunDirection"].SetValueVec3(SunDirection);
        _skyShader.Uniforms["uMoonDirection"].SetValueVec3(MoonDirection);
        _skyShader.Uniforms["uUseTexture"].SetValueFloat(UseTexture ? 1.0f : 0.0f);
        _skyShader.Uniforms["uTextureStrength"].SetValueFloat(0.5f);
        _skyShader.Uniforms["uSkyTexture"].SetValueTexture(SkyTexture ?? _assetManager.GetWhiteTexture<Texture2D>());
        _skyShader.Uniforms["uEnableAurora"].SetValueFloat(EnableAurore ? 1.0f : 0.0f);
        _skyShader.Uniforms["uAuroraIntensity"].SetValueFloat(AuroreIntensity);

        _skyShader.Uniforms["uView"].SetValueMat4(camera.View);
        _skyShader.Uniforms["uProjection"].SetValueMat4(camera.Projection);
        _skyShader.Uniforms["uWorld"].SetValueMat4(Transform.GetTransformationMatrix());

        _skyShader.Uniforms["uTime"].SetValueFloat(_timeOfDay);
        _skyShader.Uniforms["uRealTime"].SetValueFloat(gameTime.GetTotalGameTimeSeconds());

        graphicsDevice.ShaderProgram = _skyShader;
        graphicsDevice.VertexArray = _vertexBuffer;
        graphicsDevice.DrawArrays(PrimitiveType.Triangles, 0, _vertexBuffer.Value.StorageLength);

        // Restore default states
        graphicsDevice.DepthState = DepthState.Default;
        graphicsDevice.CullFaceMode = CullingMode.CullBack;
    }

    private void CreateSkyGeometry()
    {
        var uniqueVertices = new Vector3[]
        {
            new(-1, -1, 1), new(1, -1, 1), new(-1, 1, 1), new(1, 1, 1),
            new(1, -1, -1), new(-1, -1, -1), new(1, 1, -1), new(-1, 1, -1),
            new(-1, 1, 1), new(1, 1, 1), new(-1, 1, -1), new(1, 1, -1),
            new(-1, -1, -1), new(1, -1, -1), new(-1, -1, 1), new(1, -1, 1),
            new(1, -1, 1), new(1, -1, -1), new(1, 1, 1), new(1, 1, -1),
            new(-1, -1, -1), new(-1, -1, 1), new(-1, 1, -1), new(-1, 1, 1)
        };

        var indices = new short[]
        {
            0, 1, 2, 2, 1, 3,
            4, 5, 6, 6, 5, 7,
            8, 9, 10, 10, 9, 11,
            12, 13, 14, 14, 13, 15,
            16, 17, 18, 18, 17, 19,
            20, 21, 22, 22, 21, 23
        };

        var vertices = new PositionVertex[indices.Length];

        for (int i = 0; i < indices.Length; i++)
        {
            vertices[i] = new PositionVertex(uniqueVertices[indices[i]]);
        }

        _vertexBuffer = new VertexBuffer<PositionVertex>(
            _graphicsDevice,
            vertices,
            BufferUsage.StaticCopy
        );
    }
}
