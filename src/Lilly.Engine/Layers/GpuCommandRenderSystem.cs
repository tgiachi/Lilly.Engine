using Lilly.Engine.Commands;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Payloads.Shaders;
using Lilly.Engine.Rendering.Core.Types;
using Lilly.Engine.Shaders;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using TrippyGL;

namespace Lilly.Engine.Layers;

public class GpuCommandRenderSystem : BaseRenderLayerSystem<IGameObject>
{
    private readonly RenderContext _renderContext;

    private readonly IAssetManager _assetManager;

    public Color4b ClearColor { get; set; } = Color4b.BlanchedAlmond;

    /// <summary>
    /// This layer processes Clear and Window commands.
    /// </summary>
    public override IReadOnlySet<RenderCommandType> SupportedCommandTypes { get; } =
        new HashSet<RenderCommandType>
        {
            RenderCommandType.Clear,
            RenderCommandType.Window,
            RenderCommandType.UseShader
        };

    public GpuCommandRenderSystem(RenderContext renderContext, IAssetManager assetManager) : base("GpuCommandSystem", RenderLayer.Background)
    {
        _renderContext = renderContext;
        _assetManager = assetManager;
    }

    /// <summary>
    /// Collects render commands for clearing the screen and handling window operations.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <returns>A list of render commands.</returns>
    public override List<RenderCommand> CollectRenderCommands(GameTime gameTime)
    {
        RenderCommands.Clear();
        RenderCommands.Add(RenderCommandHelpers.CreateClear(new(ClearColor)));

        return RenderCommands;
    }

    /// <summary>
    /// Processes the render commands for clearing the screen and handling window operations.
    /// </summary>
    /// <param name="renderCommands">The list of render commands to process.</param>
    public override void ProcessRenderCommands(ref List<RenderCommand> renderCommands)
    {
        _renderContext.GraphicsDevice.DepthState = DepthState.Default;
        _renderContext.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        _renderContext.Gl.Enable(GLEnum.Multisample);


        foreach (var cmd in renderCommands)
        {
            switch (cmd.CommandType)
            {
                case RenderCommandType.Clear:
                    var clearPayload = cmd.GetPayload<ClearPayload>();
                    _renderContext.GraphicsDevice.ClearColor = clearPayload.Color.ToVector4();
                    _renderContext.GraphicsDevice.Clear(ClearBuffers.Color | ClearBuffers.Depth);

                    break;

                case RenderCommandType.Window:
                    var windowPayload = cmd.GetPayload<WindowPayload>();
                    ProcessWindowCommand(windowPayload);

                    break;

                case RenderCommandType.UseShader:
                    var useShaderPayload = cmd.GetPayload<UseShaderPayload>();
                    ProcessShaderCommand(useShaderPayload);

                    break;
            }
        }
        base.ProcessRenderCommands(ref renderCommands);
    }

    private void ProcessShaderCommand(UseShaderPayload payload)
    {
        if (payload.ShaderHandle != 0)
        {
            var shaderProgram = _assetManager.GetLillyShaderFromHandle(payload.ShaderHandle);
            shaderProgram.Use();

            foreach (var uniform in payload.Uniforms.Values)
            {
                SetUniformBasedOnType((OpenGlLillyShader)shaderProgram, uniform);
            }
        }
        else
        {
            _renderContext.GraphicsDevice.ShaderProgram = null;
        }
    }

    private void SetUniformBasedOnType(OpenGlLillyShader shaderProgram, Rendering.Core.Payloads.Shaders.ShaderUniform uniform)
    {
        switch (uniform.Type)
        {
            case ShaderUniformType.Float:
                if (uniform.Value is float floatValue)
                {
                    shaderProgram.SetUniform(uniform.Name, floatValue);
                }

                break;
            case ShaderUniformType.Int:
                if (uniform.Value is int intValue)
                {
                    shaderProgram.SetUniform(uniform.Name, intValue);
                }

                break;
            case ShaderUniformType.Vec2:
                if (uniform.Value is Vector2D<float> vec2Value)
                {
                    shaderProgram.SetUniform(uniform.Name, vec2Value);
                }

                break;
            case ShaderUniformType.Vec3:
                if (uniform.Value is Vector3D<float> vec3Value)
                {
                    shaderProgram.SetUniform(uniform.Name, vec3Value);
                }

                break;
            case ShaderUniformType.Vec4:
                if (uniform.Value is Vector4D<float> vec4Value)
                {
                    shaderProgram.SetUniform(uniform.Name, vec4Value);
                }

                break;
            case ShaderUniformType.Mat3:
                if (uniform.Value is Matrix3X2<float> mat3Value)
                {
                    shaderProgram.SetUniform(uniform.Name, mat3Value);
                }

                break;
            case ShaderUniformType.Mat4:
                if (uniform.Value is Matrix4X4<float> mat4Value)
                {
                    shaderProgram.SetUniform(uniform.Name, mat4Value);
                }

                break;
            case ShaderUniformType.Texture2D:
                if (uniform.Value is uint textureHandle)
                {
                    shaderProgram.SetUniform(uniform.Name, textureHandle, uniform.Unit);
                }

                break;
            case ShaderUniformType.Sampler2D:
                if (uniform.Value is uint samplerHandle)
                {
                    shaderProgram.SetUniform(uniform.Name, samplerHandle, uniform.Unit);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ProcessWindowCommand(WindowPayload payload)
    {
        switch (payload.SubCommandType)
        {
            case WindowSubCommandType.SetTitle:
                _renderContext.Window.Title = payload.Data as string;

                break;
            case WindowSubCommandType.SetFullscreen:
                if (payload.Data is bool isFullscreen) { }

                break;
            case WindowSubCommandType.SetVSync:
                if (payload.Data is bool isVSync)
                {
                    _renderContext.Window.VSync = isVSync;
                }

                break;
            case WindowSubCommandType.SetRefreshRate:
                if (payload.Data is int refreshRate)
                {
                    _renderContext.Renderer.TargetFramesPerSecond = refreshRate;
                }

                break;
        }
    }
}
