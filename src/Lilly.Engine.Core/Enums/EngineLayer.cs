namespace Lilly.Engine.Core.Enums;

/// <summary>
/// Defines the rendering layers with their priority order.
/// Lower values render first (background), higher values render last (foreground).
/// </summary>
public enum EngineLayer
{
    /// <summary>
    /// Background layer - rendered first.
    /// </summary>
    Background = 0,

    /// <summary>
    /// Input layer - handles user input processing.
    /// </summary>
    Input = 25,

    /// <summary>
    /// 3D world layer - terrain, models.
    /// </summary>
    World3D = 50,

    /// <summary>
    /// 2D world layer - game objects, sprites, tiles.
    /// </summary>
    World2D = 100,

    /// <summary>
    /// Particles and effects layer.
    /// </summary>
    Effects = 150,

    /// <summary>
    /// Game UI layer - HUD, health bars, in-game menus.
    /// </summary>
    GameUI = 200,

    /// <summary>
    /// Overlay layer - screen overlays, fade effects.
    /// </summary>
    Overlay = 800,

    /// <summary>
    /// Debug UI layer - ImGui, profiling, always on top.
    /// </summary>
    DebugUI = 900
}
