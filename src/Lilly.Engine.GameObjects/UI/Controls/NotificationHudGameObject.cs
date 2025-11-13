using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Notifications;
using Lilly.Engine.Extensions;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Utils;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.GameObjects.UI.Controls;

/// <summary>
/// Displays queued toast notifications with fade and slide animations.
/// </summary>
public class NotificationHudGameObject : BaseGameObject2D
{
    private const float DefaultSlideSpeed = 8.0f;
    private const float DefaultPadding = 10.0f;
    private const float DefaultSpacing = 35.0f;
    private const float DefaultOpacityThreshold = 0.01f;
    private const float InitialYOffset = -50f;
    private const int DefaultFontSize = 18;

    private readonly List<NotificationMessage> _messages = [];
    private readonly IAssetManager _assetManager;

    private string _fontFamily;

    /// <summary>
    /// Gets or sets the anchor position for notifications.
    /// </summary>
    public Vector2D<float> StartPosition { get; set; } = new(20f, 20f);

    /// <summary>
    /// Gets or sets the vertical spacing between notifications.
    /// </summary>
    public float MessageSpacing { get; set; } = DefaultSpacing;

    /// <summary>
    /// Gets or sets the text padding.
    /// </summary>
    public float MessagePadding { get; set; } = DefaultPadding;

    /// <summary>
    /// Gets or sets the maximum number of active notifications.
    /// </summary>
    public int MaxVisibleMessages { get; set; } = 5;

    /// <summary>
    /// Gets or sets the slide animation speed.
    /// </summary>
    public float SlideAnimationSpeed { get; set; } = DefaultSlideSpeed;

    /// <summary>
    /// Gets or sets the icon size.
    /// </summary>
    public Vector2D<float> IconSize { get; set; } = new(24f, 24f);

    /// <summary>
    /// Gets or sets the spacing between icon and text.
    /// </summary>
    public float IconSpacing { get; set; } = 8f;

    /// <summary>
    /// Gets or sets the font family to use for notification text.
    /// </summary>
    public string FontFamily
    {
        get => _fontFamily;
        set => _fontFamily = value ?? "DefaultFont";
    }

    /// <summary>
    /// Gets or sets the font size to use for notification text.
    /// </summary>
    public int FontSize { get; set; } = DefaultFontSize;

    /// <summary>
    /// Initializes a new instance of the NotificationHudGameObject class.
    /// </summary>
    /// <param name="assetManager">The asset manager service.</param>
    /// <param name="notificationService">The notification service.</param>
    public NotificationHudGameObject(
        UITheme theme,
        IAssetManager assetManager,
        INotificationService notificationService
    )
    {
        _assetManager = assetManager;

        FontFamily = theme.FontName;
        Order = 9000; // Always render on top of other UI elements

        notificationService.NotificationRaised += HandleNotificationRaised;
        notificationService.NotificationsCleared += HandleNotificationsCleared;
    }

    /// <summary>
    /// Renders the notification HUD by yielding render commands.
    /// </summary>
    /// <param name="gameTime">The game timing information.</param>
    /// <returns>An enumerable collection of render commands.</returns>
    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (_messages.Count == 0)
        {
            yield break;
        }

        for (var i = 0; i < _messages.Count; i++)
        {
            var message = _messages[i];

            if (message.Alpha <= DefaultOpacityThreshold)
            {
                continue;
            }

            // Calculate positions and sizes
            var basePosition = new Vector2D<float>(StartPosition.X, StartPosition.Y + message.YOffset);
            var textSize = TextMeasurement.MeasureString(_assetManager, message.Text, FontFamily, FontSize);
            var hasIcon = !string.IsNullOrWhiteSpace(message.IconTextureName);
            var iconWidth = hasIcon ? IconSize.X : 0f;
            var iconSpacing = hasIcon ? IconSpacing : 0f;
            var contentHeight = MathF.Max(textSize.Y, hasIcon ? IconSize.Y : 0f);

            // Background rectangle
            var backgroundRect = new Rectangle<float>(
                new Vector2D<float>(basePosition.X - MessagePadding, basePosition.Y - MessagePadding),
                new Vector2D<float>(
                    textSize.X + iconWidth + iconSpacing + MessagePadding * 2f,
                    contentHeight + MessagePadding * 2f
                )
            );

            // Text position (offset by icon if present)
            var textPosition = new Vector2D<float>(
                basePosition.X + iconWidth + iconSpacing,
                basePosition.Y + (contentHeight - textSize.Y) / 2f
            );

            // Apply alpha to colors
            var finalBackground = message.BackgroundColor.ApplyAlpha(message.Alpha);
            var finalText = message.TextColor.ApplyAlpha(message.Alpha);

            // Draw background
            if (finalBackground.A > 0)
            {
                yield return DrawRectangle(backgroundRect, finalBackground, depth: Order);
            }

            // Draw icon if present
            if (hasIcon)
            {
                var iconRect = new Rectangle<float>(
                    new Vector2D<float>(
                        basePosition.X,
                        basePosition.Y + (contentHeight - IconSize.Y) / 2f
                    ),
                    IconSize
                );

                var iconColor = new Color4b(255, 255, 255, 255).ApplyAlpha(message.Alpha);

                yield return DrawTextureCustom(
                    texture: message.IconTextureName!,
                    destination: iconRect,
                    color: iconColor,
                    depth: Order
                );
            }

            // Draw text
            yield return DrawTextCustom(
                fontFamily: FontFamily,
                text: message.Text,
                fontSize: FontSize,
                position: textPosition,
                color: finalText,
                depth: Order
            );
        }
    }

    /// <summary>
    /// Updates the notification animations and lifecycle.
    /// </summary>
    /// <param name="gameTime">The game timing information.</param>
    public override void Update(GameTime gameTime)
    {
        if (_messages.Count == 0)
        {
            return;
        }

        var deltaTime = gameTime.GetElapsedSeconds();

        // Update all messages (backwards to allow safe removal)
        for (var i = _messages.Count - 1; i >= 0; i--)
        {
            var message = _messages[i];
            message.ElapsedTime += deltaTime;

            // Update alpha based on fade state
            if (message.IsFadingIn)
            {
                var fadeProgress = message.ElapsedTime / message.FadeInDuration;
                message.Alpha = MathF.Min(1.0f, fadeProgress);
            }
            else if (message.IsFadingOut)
            {
                var fadeOutStart = message.Duration - message.FadeOutDuration;
                var fadeProgress = (message.ElapsedTime - fadeOutStart) / message.FadeOutDuration;
                message.Alpha = MathF.Max(0.0f, 1.0f - fadeProgress);
            }
            else
            {
                message.Alpha = 1.0f;
            }

            // Update slide animation
            var targetY = message.TargetY;
            var currentY = StartPosition.Y + message.YOffset;
            var difference = targetY - currentY;

            if (MathF.Abs(difference) > 0.5f)
            {
                message.YOffset += difference * SlideAnimationSpeed * deltaTime;
            }
            else
            {
                message.YOffset = targetY - StartPosition.Y;
            }

            // Remove expired messages
            if (message.ShouldRemove)
            {
                _messages.RemoveAt(i);
                UpdateMessagePositions();
            }
        }

        // Enforce max visible messages
        while (_messages.Count > MaxVisibleMessages)
        {
            _messages.RemoveAt(0);
            UpdateMessagePositions();
        }
    }

    /// <summary>
    /// Handles the NotificationRaised event.
    /// </summary>
    private void HandleNotificationRaised(object? sender, NotificationMessage message)
    {
        // Create a new notification message instance
        var instance = new NotificationMessage
        {
            Text = message.Text,
            Duration = message.Duration,
            TextColor = message.TextColor,
            BackgroundColor = message.BackgroundColor,
            FadeInDuration = message.FadeInDuration,
            FadeOutDuration = message.FadeOutDuration,
            ElapsedTime = 0f,
            Alpha = 0f,
            YOffset = InitialYOffset,
            TargetY = 0f,
            IconTextureName = message.IconTextureName,
            IconTexture = null // Will be resolved by texture name during rendering
        };

        _messages.Add(instance);
        UpdateMessagePositions();
    }

    /// <summary>
    /// Handles the NotificationsCleared event.
    /// </summary>
    private void HandleNotificationsCleared(object? sender, EventArgs e)
    {
        _messages.Clear();
    }

    /// <summary>
    /// Updates the target Y positions for all messages to create slide animation.
    /// </summary>
    private void UpdateMessagePositions()
    {
        for (var i = 0; i < _messages.Count; i++)
        {
            _messages[i].TargetY = StartPosition.Y + i * MessageSpacing;
        }
    }
}
