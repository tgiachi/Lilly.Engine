using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Types;
using Silk.NET.Maths;

namespace Lilly.Engine.Commands;

/// <summary>
/// Provides helper methods for creating render commands.
/// </summary>
public static class RenderCommandHelpers
{
    /// <summary>
    /// Creates a render command for clearing the screen.
    /// </summary>
    /// <param name="payload">The clear payload containing the clear operation details.</param>
    /// <returns>A render command configured for clear operations.</returns>
    public static RenderCommand CreateClear(ClearPayload payload)
        => new(RenderCommandType.Clear, payload);

    /// <summary>
    /// Creates a render command for disabling the scissor test.
    /// </summary>
    /// <returns></returns>
    public static RenderCommand CreateDisableScissor()
        => new(RenderCommandType.Scissor, new ScissorPayload());

    /// <summary>
    /// Creates a render command for drawing text.
    /// </summary>
    /// <param name="payload">The draw text payload containing text rendering details.</param>
    /// <returns>A render command configured for text rendering.</returns>
    public static RenderCommand CreateDrawText(DrawTextPayload payload)
        => new(RenderCommandType.DrawText, payload);

    /// <summary>
    /// Creates a render command for drawing a texture.
    /// </summary>
    /// <param name="payload">The draw texture payload containing texture rendering details.</param>
    /// <returns>A render command configured for texture rendering.</returns>
    public static RenderCommand CreateDrawTexture(DrawTexturePayload payload)
        => new(RenderCommandType.DrawTexture, payload);

    /// <summary>
    /// Creates a render command for setting the scissor rectangle.
    /// </summary>
    /// <param name="rectangle"></param>
    /// <returns></returns>
    public static RenderCommand CreateScissor(Rectangle<int> rectangle)
        => new(RenderCommandType.Scissor, new ScissorPayload(rectangle));

    /// <summary>
    /// Creates a render command for window operations.
    /// </summary>
    /// <param name="payload">The window payload containing window operation details.</param>
    /// <returns>A render command configured for window operations.</returns>
    public static RenderCommand CreateWindow(WindowPayload payload)
        => new(RenderCommandType.Window, payload);

    /// <summary>
    /// Creates a render command for ImGui rendering.
    /// </summary>
    /// <param name="payload">The ImGui data payload containing UI rendering details.</param>
    /// <returns>A render command configured for ImGui operations.</returns>
    public static RenderCommand ImGuiRender(ImGuiDataPayload payload)
        => new(RenderCommandType.ImGui, payload);

    /// <summary>
    /// Creates a window command to set the refresh rate.
    /// </summary>
    /// <param name="refreshRate">The refresh rate in Hz.</param>
    /// <returns>A render command to set the refresh rate.</returns>
    public static RenderCommand SetRefreshRate(int refreshRate)
        => CreateWindow(
            new()
            {
                SubCommandType = WindowSubCommandType.SetRefreshRate,
                Data = refreshRate
            }
        );

    /// <summary>
    /// Creates a window command to set fullscreen mode.
    /// </summary>
    /// <param name="enabled">True to enable fullscreen, false to disable.</param>
    /// <returns>A render command to set fullscreen state.</returns>
    public static RenderCommand SetWindowFullscreen(bool enabled)
        => CreateWindow(
            new()
            {
                SubCommandType = WindowSubCommandType.SetFullscreen,
                Data = enabled
            }
        );

    /// <summary>
    /// Creates a window command to set the window size.
    /// </summary>
    /// <param name="width">The window width.</param>
    /// <param name="height">The window height.</param>
    /// <returns>A render command to set the window size.</returns>
    public static RenderCommand SetWindowSize(int width, int height)
        => CreateWindow(
            new()
            {
                SubCommandType = WindowSubCommandType.SetSize,
                Data = (width, height)
            }
        );

    /// <summary>
    /// Creates a window command to set the window title.
    /// </summary>
    /// <param name="title">The new window title.</param>
    /// <returns>A render command to set the window title.</returns>
    public static RenderCommand SetWindowTitle(string title)
        => CreateWindow(
            new()
            {
                SubCommandType = WindowSubCommandType.SetTitle,
                Data = title
            }
        );

    /// <summary>
    /// Creates a window command to enable or disable VSync.
    /// </summary>
    /// <param name="enabled">True to enable VSync, false to disable.</param>
    /// <returns>A render command to set VSync state.</returns>
    public static RenderCommand SetWindowVSync(bool enabled)
        => CreateWindow(
            new()
            {
                SubCommandType = WindowSubCommandType.SetVSync,
                Data = enabled
            }
        );
}
