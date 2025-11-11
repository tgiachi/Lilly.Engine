namespace Lilly.Engine.Rendering.Core.Types;

/// <summary>
/// Represents mouse buttons that can be detected by the input manager.
/// Includes primary buttons (left, right, middle) and extended buttons (X1, X2).
/// </summary>
public enum MouseButton
{
    /// <summary>
    /// Left mouse button (primary click).
    /// </summary>
    Left = 0,

    /// <summary>
    /// Right mouse button (context menu).
    /// </summary>
    Right = 1,

    /// <summary>
    /// Middle mouse button (typically wheel click).
    /// </summary>
    Middle = 2,

    /// <summary>
    /// First extended mouse button (typically forward/back button on gaming mice).
    /// </summary>
    XButton1 = 3,

    /// <summary>
    /// Second extended mouse button (typically forward/back button on gaming mice).
    /// </summary>
    XButton2 = 4
}
