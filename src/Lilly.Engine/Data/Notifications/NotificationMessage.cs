using TrippyGL;

namespace Lilly.Engine.Data.Notifications;

/// <summary>
/// Represents a notification payload with animation state.
/// </summary>
public class NotificationMessage
{
    /// <summary>
    /// Gets or sets the notification text content.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text colour.
    /// </summary>
    public Color4b TextColor { get; set; } = Color4b.White;

    /// <summary>
    /// Gets or sets the background colour.
    /// </summary>
    public Color4b BackgroundColor { get; set; } = Color4b.Black;

    /// <summary>
    /// Gets or sets the display duration in seconds.
    /// </summary>
    public float Duration { get; set; } = 3.0f;

    /// <summary>
    /// Gets or sets the elapsed time in seconds.
    /// </summary>
    public float ElapsedTime { get; set; }

    /// <summary>
    /// Gets or sets the fade-in duration in seconds.
    /// </summary>
    public float FadeInDuration { get; set; } = 0.3f;

    /// <summary>
    /// Gets or sets the fade-out duration in seconds.
    /// </summary>
    public float FadeOutDuration { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the current alpha value.
    /// </summary>
    public float Alpha { get; set; }

    /// <summary>
    /// Gets or sets the current Y offset for slide animation.
    /// </summary>
    public float YOffset { get; set; } = -50f;

    /// <summary>
    /// Gets or sets the target Y position.
    /// </summary>
    public float TargetY { get; set; }

    /// <summary>
    /// Gets or sets the texture name for an optional icon.
    /// </summary>
    public string? IconTextureName { get; set; }

    /// <summary>
    /// Gets or sets the optional icon texture.
    /// </summary>
    public Texture2D? IconTexture { get; set; }

    /// <summary>
    /// Gets a value indicating whether the notification should be removed.
    /// </summary>
    public bool ShouldRemove => ElapsedTime >= Duration;

    /// <summary>
    /// Gets a value indicating whether the notification is fading in.
    /// </summary>
    public bool IsFadingIn => ElapsedTime < FadeInDuration;

    /// <summary>
    /// Gets a value indicating whether the notification is fading out.
    /// </summary>
    public bool IsFadingOut => ElapsedTime >= Duration - FadeOutDuration;
}
