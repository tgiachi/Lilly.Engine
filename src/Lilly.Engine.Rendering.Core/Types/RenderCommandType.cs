namespace Lilly.Engine.Rendering.Core.Types;

/// <summary>
/// Defines the types of rendering commands that can be executed.
/// </summary>
public enum RenderCommandType : byte
{
    /// <summary>
    /// Clear the screen or render target.
    /// </summary>
    Clear,

    /// <summary>
    ///  Execute a low-level GPU command.
    /// </summary>
    GpuCommand,

    /// <summary>
    /// Set window options
    /// </summary>
    Window,

    /// <summary>
    /// Set scissor rectangle for rendering.
    /// </summary>
    Scissor,

    /// <summary>
    /// Draw text on the screen.
    /// </summary>
    DrawText,

    /// <summary>
    /// Draw a texture or sprite.
    /// </summary>
    DrawTexture,

    /// <summary>
    ///  Draw a mesh or 3D model.
    /// </summary>
    DrawArray,
    /// <summary>
    ///  Draw an indexed mesh or 3D model.
    /// </summary>
    DrawElements,

    /// <summary>
    ///  Use a specific shader program.
    /// </summary>
    UseShader,

    /// <summary>
    /// Set depth buffer state.
    /// </summary>
    SetDepthState,

    /// <summary>
    /// Set face culling mode.
    /// </summary>
    SetCullMode,

    /// <summary>
    /// Set shader uniforms for a specific program.
    /// </summary>
    SetUniforms,

    /// <summary>
    /// Render ImGui debug interface.
    /// </summary>
    ImGui,

    /// <summary>
    /// No specific render command.
    /// </summary>
    None
}
