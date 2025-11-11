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
    ///  Set window options
    /// </summary>
    Window,

    /// <summary>
    ///  Set scissor rectangle for rendering.
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
    /// Render ImGui debug interface.
    /// </summary>
    ImGui,
    /// <summary>
    ///  No specific render command.
    /// </summary>
    None
}
