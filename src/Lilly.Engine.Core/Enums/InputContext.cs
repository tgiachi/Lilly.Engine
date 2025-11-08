namespace Lilly.Engine.Core.Enums;

/// <summary>
/// Defines the current input context/mode for the application.
/// </summary>
public enum InputContext
{
    /// <summary>
    /// No input context.
    /// </summary>
    None,

    /// <summary>
    /// UI has priority (menus, dialogs).
    /// </summary>
    UI,

    /// <summary>
    /// 3D world/camera has priority.
    /// </summary>
    Gameplay3D,

    /// <summary>
    /// 2D gameplay has priority.
    /// </summary>
    Gameplay2D,

    /// <summary>
    /// Debug console/ImGui has priority.
    /// </summary>
    Debug,

    /// <summary>
    /// Game is paused, only UI input allowed.
    /// </summary>
    Paused
}
