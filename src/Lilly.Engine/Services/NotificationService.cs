using Lilly.Engine.Data.Notifications;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Types;
using TrippyGL;

namespace Lilly.Engine.Services;

/// <summary>
/// Provides an implementation of notification publishing.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private const float DefaultDuration = 3.0f;
    private const float WarningDuration = 4.0f;
    private const float ErrorDuration = 5.0f;
    private static readonly Color4b DefaultBackground = new(0, 0, 0, 180);
    private static readonly Color4b DefaultText = Color4b.White;
    private static readonly Color4b InfoBackground = new(0, 100, 200, 180);
    private static readonly Color4b SuccessBackground = new(0, 150, 0, 180);
    private static readonly Color4b WarningBackground = new(200, 150, 0, 180);
    private static readonly Color4b ErrorBackground = new(200, 0, 0, 180);
    private readonly Lock _lock = new();

    /// <inheritdoc />
    public event EventHandler<NotificationMessage>? NotificationRaised;

    /// <inheritdoc />
    public event EventHandler? NotificationsCleared;

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            NotificationsCleared?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public void ShowError(string text, float? duration = null, string? iconTextureName = null)
    {
        ShowMessage(text, NotificationType.Error, duration, iconTextureName);
    }

    /// <inheritdoc />
    public void ShowInfo(string text, float? duration = null, string? iconTextureName = null)
    {
        ShowMessage(text, NotificationType.Info, duration, iconTextureName);
    }

    /// <inheritdoc />
    public void ShowMessage(
        string text,
        float? duration = null,
        Color4b? textColor = null,
        Color4b? backgroundColor = null,
        string? iconTextureName = null
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var message = CreateMessage(
            text.Trim(),
            duration ?? DefaultDuration,
            textColor ?? DefaultText,
            backgroundColor ?? DefaultBackground,
            iconTextureName
        );
        Publish(message);
    }

    /// <inheritdoc />
    public void ShowMessage(string text, NotificationType type, float? duration = null, string? iconTextureName = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var (textColor, backgroundColor, defaultDuration) = GetDefaults(type);
        var message = CreateMessage(text.Trim(), duration ?? defaultDuration, textColor, backgroundColor, iconTextureName);
        Publish(message);
    }

    /// <inheritdoc />
    public void ShowSuccess(string text, float? duration = null, string? iconTextureName = null)
    {
        ShowMessage(text, NotificationType.Success, duration, iconTextureName);
    }

    /// <inheritdoc />
    public void ShowWarning(string text, float? duration = null, string? iconTextureName = null)
    {
        ShowMessage(text, NotificationType.Warning, duration, iconTextureName);
    }

    private static NotificationMessage CreateMessage(
        string text,
        float duration,
        Color4b textColor,
        Color4b backgroundColor,
        string? iconTextureName
    )
    {
        var sanitizedIconName = string.IsNullOrWhiteSpace(iconTextureName)
                                    ? null
                                    : iconTextureName.Trim();

        return new()
        {
            Text = text,
            Duration = duration,
            TextColor = textColor,
            BackgroundColor = backgroundColor,
            IconTextureName = sanitizedIconName
        };
    }

    private static (Color4b TextColor, Color4b BackgroundColor, float Duration) GetDefaults(NotificationType type)
    {
        return type switch
        {
            NotificationType.Info    => (Color4b.White, InfoBackground, DefaultDuration),
            NotificationType.Success => (Color4b.White, SuccessBackground, DefaultDuration),
            NotificationType.Warning => (Color4b.White, WarningBackground, WarningDuration),
            NotificationType.Error   => (Color4b.White, ErrorBackground, ErrorDuration),
            _                        => (DefaultText, DefaultBackground, DefaultDuration)
        };
    }

    private void Publish(NotificationMessage message)
    {
        lock (_lock)
        {
            var copy = CreateMessage(
                message.Text,
                message.Duration,
                message.TextColor,
                message.BackgroundColor,
                message.IconTextureName
            );
            copy.FadeInDuration = message.FadeInDuration;
            copy.FadeOutDuration = message.FadeOutDuration;
            NotificationRaised?.Invoke(this, copy);
        }
    }
}
