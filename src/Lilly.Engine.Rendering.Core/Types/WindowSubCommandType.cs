namespace Lilly.Engine.Rendering.Core.Types;

/// <summary>
/// Defines the types of window operations that can be performed.
/// </summary>
public enum WindowSubCommandType : byte
{
    /// <summary>
    /// Sets the window title.
    /// </summary>
    SetTitle,

    /// <summary>
    /// Sets the window size (width and height).
    /// </summary>
    SetSize,

    /// <summary>
    /// Enables or disables vertical synchronization (VSync).
    /// </summary>
    SetVSync,

    /// <summary>
    /// Enables or disables fullscreen mode.
    /// </summary>
    SetFullscreen,

    /// <summary>
    ///  Sets the window refresh rate.
    /// </summary>
    SetRefreshRate
}
