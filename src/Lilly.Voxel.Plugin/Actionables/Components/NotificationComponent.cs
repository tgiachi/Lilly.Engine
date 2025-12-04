using Lilly.Engine.Types;
using Lilly.Voxel.Plugin.Interfaces.Actionables;

namespace Lilly.Voxel.Plugin.Actionables.Components;

public readonly struct NotificationComponent(
    string Message,
    NotificationType NotificationType = NotificationType.Default,
    string TextureId = ""
) : IActionableComponent;
