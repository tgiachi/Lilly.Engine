using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Notifications;
using Lilly.Engine.Extensions;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Utils;
using Lilly.Rendering.Core.Interfaces.Services;
using TrippyGL;

namespace Lilly.Engine.GameObjects.UI.Controls;

/// <summary>
/// Displays queued toast notifications with fade and slide animations.
/// </summary>
public class NotificationHudGameObject : Base2dGameObject
{
    private const float DefaultSlideSpeed = 8.0f;
    private const float DefaultPadding = 10.0f;
    private const float DefaultSpacing = 35.0f;
    private const float DefaultOpacityThreshold = 0.01f;
    private const float InitialYOffset = -50f;
    private const int DefaultFontSize = 18;
    private const float MinNotificationWidth = 300.0f;

    private readonly List<NotificationMessage> _messages = [];
    private readonly IAssetManager _assetManager;

    private string _fontFamily;

    /// <summary>
    /// Gets or sets the anchor position for notifications.
    /// </summary>
    public Vector2 StartPosition { get; set; } = new(20f, 20f);

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
    public Vector2 IconSize { get; set; } = new(24f, 24f);

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
    /// <param name="theme">The UI theme for styling.</param>
    /// <param name="assetManager">The asset manager service.</param>
    /// <param name="notificationService">The notification service.</param>
    public NotificationHudGameObject(
        UITheme theme,
        IAssetManager assetManager,
        INotificationService notificationService,
        IRenderPipeline gameObjectManager
    ) : base("NotificationHud", gameObjectManager, 9000) // High Z-index to render on top
    {
        _assetManager = assetManager;
        _fontFamily = theme.FontName;

        notificationService.NotificationRaised += HandleNotificationRaised;
        notificationService.NotificationsCleared += HandleNotificationsCleared;
    }

    /// <summary>
    /// Updates the notification animations and lifecycle.
    /// </summary>
    /// <param name="gameTime">The game timing information.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

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
    /// Renders the notification HUD using the sprite batcher.
    /// </summary>
    /// <param name="gameTime">The game timing information.</param>
    protected override void OnDraw(GameTime gameTime)
    {
        if (_messages.Count == 0 || SpriteBatcher == null)
        {
            return;
        }

        // Pre-pass: Calculate total bounds for proper Transform sizing
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;

        for (var i = 0; i < _messages.Count; i++)
        {
            var message = _messages[i];

            if (message.Alpha <= DefaultOpacityThreshold)
            {
                continue;
            }

            var basePosition = new Vector2(StartPosition.X, StartPosition.Y + message.YOffset);
            var textSizeVec2D = TextMeasurement.MeasureString(_assetManager, message.Text, FontFamily, FontSize);
            var textSize = new Vector2(textSizeVec2D.X, textSizeVec2D.Y);
            var hasIcon = !string.IsNullOrWhiteSpace(message.IconTextureName);
            var iconWidth = hasIcon ? IconSize.X : 0f;
            var iconSpacing = hasIcon ? IconSpacing : 0f;
            var contentHeight = MathF.Max(textSize.Y, hasIcon ? IconSize.Y : 0f);

            var calculatedWidth = textSize.X + iconWidth + iconSpacing + MessagePadding * 2f;
            var width = MathF.Max(calculatedWidth, MinNotificationWidth);
            var height = contentHeight + MessagePadding * 2f;

            var rectX = basePosition.X - MessagePadding;
            var rectY = basePosition.Y - MessagePadding;

            minX = MathF.Min(minX, rectX);
            minY = MathF.Min(minY, rectY);
            maxX = MathF.Max(maxX, rectX + width);
            maxY = MathF.Max(maxY, rectY + height);
        }

        // Update Transform for proper bounds tracking
        if (minX != float.MaxValue)
        {
            Transform.Position = new(minX, minY);
            Transform.Size = new(maxX - minX, maxY - minY);
        }

        // Rendering pass
        for (var i = 0; i < _messages.Count; i++)
        {
            var message = _messages[i];

            if (message.Alpha <= DefaultOpacityThreshold)
            {
                continue;
            }

            // Calculate positions and sizes
            var basePosition = new Vector2(StartPosition.X, StartPosition.Y + message.YOffset);
            var textSizeVec2D = TextMeasurement.MeasureString(_assetManager, message.Text, FontFamily, FontSize);
            var textSize = new Vector2(textSizeVec2D.X, textSizeVec2D.Y);
            var hasIcon = !string.IsNullOrWhiteSpace(message.IconTextureName);
            var iconWidth = hasIcon ? IconSize.X : 0f;
            var iconSpacing = hasIcon ? IconSpacing : 0f;
            var contentHeight = MathF.Max(textSize.Y, hasIcon ? IconSize.Y : 0f);

            // Background rectangle
            var calculatedWidth = textSize.X + iconWidth + iconSpacing + MessagePadding * 2f;
            var backgroundWidth = MathF.Max(calculatedWidth, MinNotificationWidth);
            var backgroundHeight = contentHeight + MessagePadding * 2f;
            var backgroundPosition = new Vector2(
                basePosition.X - MessagePadding,
                basePosition.Y - MessagePadding
            );
            var backgroundSize = new Vector2(backgroundWidth, backgroundHeight);

            // Text position (offset by icon if present)
            var textPosition = new Vector2(
                basePosition.X + iconWidth + iconSpacing,
                basePosition.Y + (contentHeight - textSize.Y) / 2f
            );

            // Apply alpha to colors
            var finalBackground = message.BackgroundColor.ApplyAlpha(message.Alpha);
            var finalText = message.TextColor.ApplyAlpha(message.Alpha);

            // Draw background
            if (finalBackground.A > 0)
            {
                SpriteBatcher.DrawRectangle(
                    backgroundPosition,
                    backgroundSize,
                    finalBackground,
                    0f,
                    Vector2.Zero
                );
            }

            // Draw icon if present
            if (hasIcon)
            {
                var iconPosition = new Vector2(
                    basePosition.X,
                    basePosition.Y + (contentHeight - IconSize.Y) / 2f
                );

                var iconColor = new Color4b(255, 255, 255).ApplyAlpha(message.Alpha);

                SpriteBatcher.DrawTexure(
                    message.IconTextureName!,
                    iconPosition,
                    null,
                    null,
                    iconColor,
                    null,
                    new Vector2(IconSize.X / 32f, IconSize.Y / 32f), // Assuming 32x32 base texture
                    null,
                    0.1f
                );
            }

            // Draw text
            SpriteBatcher.DrawText(
                FontFamily,
                FontSize,
                message.Text,
                textPosition,
                finalText,
                0f,
                Vector2.One
            );
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
