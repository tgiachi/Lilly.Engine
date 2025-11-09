namespace Lilly.Engine.Core.Enums;

/// <summary>
/// Defines the types of assets supported by the engine.
/// </summary>
public enum AssetType
{
    /// <summary>
    /// Texture asset (images, sprites, etc.).
    /// </summary>
    Texture,

    /// <summary>
    /// Sound asset (audio files, music, sound effects).
    /// </summary>
    Sound,

    /// <summary>
    /// Font asset (TrueType fonts, bitmap fonts).
    /// </summary>
    Font,

    /// <summary>
    /// 3D model asset (meshes, animations).
    /// </summary>
    Model,

    /// <summary>
    /// Shader asset (vertex shaders, fragment shaders, compute shaders).
    /// </summary>
    Shader
}
