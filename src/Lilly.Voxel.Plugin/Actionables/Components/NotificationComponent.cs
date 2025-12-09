using Lilly.Engine.Types;
using Lilly.Voxel.Plugin.Interfaces.Actionables;

namespace Lilly.Voxel.Plugin.Actionables.Components;

/// <summary>
/// Represents a notification action component that can be associated with a block or entity.
/// </summary>
/// <param name="Message"></param>
/// <param name="NotificationType"></param>
/// <param name="TextureId"></param>
public readonly record struct NotificationComponent(
    string Message,
    NotificationType NotificationType = NotificationType.Default,
    string TextureId = ""
) : IActionableComponent;
