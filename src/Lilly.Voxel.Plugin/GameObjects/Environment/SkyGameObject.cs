using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Helpers;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Payloads.GpuSubCommands;
using Lilly.Engine.Rendering.Core.Types;
using Lilly.Engine.Shaders;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects.Environment;

public class SkyGameObject : BaseGameObject3D
{
    private readonly ShaderProgram _skyShader;
    private readonly IAssetManager _assetManager;
    private VertexBuffer<PositionVertex> _vertexBuffer;
    private float _timeOfDay;

    public Texture2D? SkyTexture { get; set; }

    public Vector3D<float> SunDirection { get; set; } = new Vector3D<float>(0.5f, 0.7f, 0.3f);
    public Vector3D<float> MoonDirection { get; set; } = new Vector3D<float>(-0.5f, -0.7f, -0.3f);

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


    public SkyGameObject(GraphicsDevice graphicsDevice, IAssetManager assetManager) : base(graphicsDevice)
    {
        IgnoreFrustumCulling = true;
        _assetManager = assetManager;
        _skyShader = assetManager.GetShaderProgram("dynamicSky");
        Order = 0;
        IsVisible = true;
    }

    public override void Initialize()
    {
        CreateSkyGeometry();
        base.Initialize();
    }

    public override void Update(GameTime gameTime)
    {
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

        SunDirection = new Vector3D<float>(
            MathF.Cos(sunAngle),
            sunHeight,
            MathF.Sin(sunAngle) * 0.3f
        );
        SunDirection = Vector3D.Normalize(SunDirection);

        float moonAngle = sunAngle + MathF.PI;
        float moonHeight = MathF.Sin(moonAngle);
        MoonDirection = new Vector3D<float>(
            MathF.Cos(moonAngle),
            moonHeight,
            MathF.Sin(moonAngle) * 0.3f
        );
        MoonDirection = Vector3D.Normalize(MoonDirection);
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        yield return RenderCommandHelpers.SetDepthState(SetDepthStatePayload.SkyboxDepthState());
        yield return RenderCommandHelpers.SetCullMode(SetCullModePayload.None());

        yield return new RenderCommand(
            RenderCommandType.DrawArray,
            new DrawArrayPayload(
                _skyShader,
                _vertexBuffer,
                _vertexBuffer.StorageLength,
                PrimitiveType.Triangles
            )
        );

        yield return RenderCommandHelpers.SetDepthState(SetDepthStatePayload.DefaultDepthState());
        yield return RenderCommandHelpers.SetCullMode(SetCullModePayload.Back());
    }

    private void CreateSkyGeometry()
    {
        var uniqueVertices = new Vector3D<float>[]
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
            GraphicsDevice,
            vertices,
            BufferUsage.StaticCopy
        );
    }

    public override void Draw(ICamera3D camera, GameTime gameTime)
    {
        const float skyboxSize = 500f;
        Transform.Position = camera.Position;
        Transform.Scale = new Vector3D<float>(skyboxSize, skyboxSize, skyboxSize);

        _skyShader.Uniforms["uSunDirection"].SetValueVec3(SunDirection.ToSystem());
        _skyShader.Uniforms["uMoonDirection"].SetValueVec3(MoonDirection.ToSystem());
        _skyShader.Uniforms["uUseTexture"].SetValueFloat(UseTexture ? 1.0f : 0.0f);
        _skyShader.Uniforms["uTextureStrength"].SetValueFloat(0.5f);
        _skyShader.Uniforms["uSkyTexture"].SetValueTexture(SkyTexture ?? _assetManager.GetWhiteTexture<Texture2D>());
        _skyShader.Uniforms["uEnableAurora"].SetValueFloat(EnableAurore ? 1.0f : 0.0f);
        _skyShader.Uniforms["uAuroraIntensity"].SetValueFloat(AuroreIntensity);

        _skyShader.Uniforms["uView"].SetValueMat4(camera.View.ToSystem());
        _skyShader.Uniforms["uProjection"].SetValueMat4(camera.Projection.ToSystem());
        _skyShader.Uniforms["uWorld"].SetValueMat4(Transform.GetTransformationMatrix().ToSystem());

        _skyShader.Uniforms["uTime"].SetValueFloat(_timeOfDay);
        _skyShader.Uniforms["uRealTime"].SetValueFloat(gameTime.GetTotalGameTimeSeconds());

        base.Draw(camera, gameTime);
    }
}
