using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Atlas;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Vertexts;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Services;
using Serilog;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

public class SkyGameObject : Base3dGameObject
{
    private readonly ShaderProgram _skyShader;
    private readonly IAssetManager _assetManager;
    private VertexBuffer<PositionVertex>? _vertexBuffer;
    private float _timeOfDay;
    private float _previousTimeOfDay;
    public Texture2D? SkyTexture { get; set; }
    public Vector3 SunDirection { get; set; } = new(0.5f, 0.7f, 0.3f);
    public Vector3 MoonDirection { get; set; } = new(-0.5f, -0.7f, -0.3f);

    private const string SunMoonAtlasKey = "sun_moon";
    private const string SunMoonAtlasTextureKey = $"{SunMoonAtlasKey}_atlas";
    private const int MoonPhaseStartIndex = 2;
    private const int MoonPhaseEndIndex = 9;
    private int _moonPhaseIndex = MoonPhaseStartIndex;
    private AtlasRegion _sunDayRegion;
    private AtlasRegion _sunHorizonRegion;
    private AtlasRegion _moonRegion;
    private Texture2D? _sunMoonAtlasTexture;

    public bool UseTexture { get; set; }
    public bool EnableAurore { get; set; } = true;
    public float AuroreIntensity { get; set; } = 0.7f;
    public bool EnableCycle { get; set; } = true;
    public float CycleSpeed { get; set; } = 0.01f;
    public bool AutoCycleMoonPhases { get; set; } = true;

    public int MoonPhaseIndex
    {
        get => _moonPhaseIndex;
        set
        {
            var clamped = Math.Clamp(value, MoonPhaseStartIndex, MoonPhaseEndIndex);

            if (_moonPhaseIndex == clamped)
            {
                return;
            }

            _moonPhaseIndex = clamped;
            UpdateMoonRegion();
        }
    }

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

    public override void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera)
    {
        if (!IsActive || _vertexBuffer == null)
        {
            return;
        }

        const float skyboxSize = 500f;
        Transform.Position = camera.Position;
        Transform.Scale = new(skyboxSize, skyboxSize, skyboxSize);

        graphicsDevice.DepthState = new(true, DepthFunction.LessOrEqual); // Skybox depth state
        graphicsDevice.FaceCullingEnabled = false;
        _skyShader.Uniforms["uSunDirection"].SetValueVec3(SunDirection);
        _skyShader.Uniforms["uMoonDirection"].SetValueVec3(MoonDirection);
        _skyShader.Uniforms["uUseTexture"].SetValueFloat(UseTexture ? 1.0f : 0.0f);
        _skyShader.Uniforms["uTextureStrength"].SetValueFloat(0.5f);
        _skyShader.Uniforms["uSkyTexture"].SetValueTexture(SkyTexture ?? _assetManager.GetWhiteTexture<Texture2D>());
        _skyShader.Uniforms["uSunMoonAtlas"]
                  .SetValueTexture(_sunMoonAtlasTexture ?? _assetManager.GetWhiteTexture<Texture2D>());
        _skyShader.Uniforms["uSunDayBase"].SetValueVec2(ToVector2(_sunDayRegion.Position));
        _skyShader.Uniforms["uSunDaySize"].SetValueVec2(ToVector2(_sunDayRegion.Size));
        _skyShader.Uniforms["uSunHorizonBase"].SetValueVec2(ToVector2(_sunHorizonRegion.Position));
        _skyShader.Uniforms["uSunHorizonSize"].SetValueVec2(ToVector2(_sunHorizonRegion.Size));
        _skyShader.Uniforms["uMoonBase"].SetValueVec2(ToVector2(_moonRegion.Position));
        _skyShader.Uniforms["uMoonSize"].SetValueVec2(ToVector2(_moonRegion.Size));
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

    public override void Initialize()
    {
        CreateSkyGeometry();
        InitializeAtlasRegions();
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }

        _previousTimeOfDay = _timeOfDay;

        if (EnableCycle)
        {
            _timeOfDay += gameTime.GetElapsedSeconds() * CycleSpeed;
            _timeOfDay %= 1.0f;
        }

        if (AutoCycleMoonPhases && _timeOfDay < _previousTimeOfDay)
        {
            AdvanceMoonPhase();
        }

        UpdateLighting();

        base.Update(gameTime);
    }

    private void AdvanceMoonPhase()
    {
        var nextPhase = _moonPhaseIndex + 1;

        if (nextPhase > MoonPhaseEndIndex)
        {
            nextPhase = MoonPhaseStartIndex;
        }

        _moonPhaseIndex = nextPhase;
        UpdateMoonRegion();
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

        for (var i = 0; i < indices.Length; i++)
        {
            vertices[i] = new(uniqueVertices[indices[i]]);
        }

        _vertexBuffer = new VertexBuffer<PositionVertex>(
            _graphicsDevice,
            vertices,
            BufferUsage.StaticCopy
        );
    }

    private void InitializeAtlasRegions()
    {
        try
        {
            _sunMoonAtlasTexture = _assetManager.GetTexture<Texture2D>(SunMoonAtlasTextureKey);
            _sunDayRegion = _assetManager.GetAtlasRegion(SunMoonAtlasKey, 0);
            _sunHorizonRegion = _assetManager.GetAtlasRegion(SunMoonAtlasKey, 1);
            UpdateMoonRegion();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unable to initialize sun/moon atlas, sky will fallback to procedural discs.");
            _sunDayRegion = default;
            _sunHorizonRegion = default;
            _moonRegion = default;
            _sunMoonAtlasTexture = null;
        }
    }

    private static Vector2 ToVector2(Vector2D<float> value)
        => new(value.X, value.Y);

    private void UpdateLighting()
    {
        const float TwoPi = MathF.PI * 2.0f;
        var sunAngle = _timeOfDay * TwoPi;
        var sunHeight = MathF.Sin(sunAngle);

        SunDirection = new(
            MathF.Cos(sunAngle),
            sunHeight,
            MathF.Sin(sunAngle) * 0.3f
        );
        SunDirection = Vector3.Normalize(SunDirection);

        var moonAngle = sunAngle + MathF.PI;
        var moonHeight = MathF.Sin(moonAngle);
        MoonDirection = new(
            MathF.Cos(moonAngle),
            moonHeight,
            MathF.Sin(moonAngle) * 0.3f
        );
        MoonDirection = Vector3.Normalize(MoonDirection);
    }

    private void UpdateMoonRegion()
    {
        if (_sunMoonAtlasTexture == null)
        {
            return;
        }

        _moonRegion = _assetManager.GetAtlasRegion(SunMoonAtlasKey, _moonPhaseIndex);
    }
}
