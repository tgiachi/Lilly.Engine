# Notification System

The Notification System allows you to display temporary "toast" messages to the user. It is built on a decoupled architecture where a service publishes messages and a UI component renders them.

## Overview

- **Service**: `INotificationService` (and its implementation `NotificationService`) handles the logic of creating and queuing messages.
- **UI**: `NotificationHudGameObject` subscribes to the service and handles the actual rendering and animation of the notifications.

## Usage

### 1. Show Notifications

Inject `INotificationService` into your class and use the helper methods:

```csharp
public class MyService
{
    private readonly INotificationService _notifications;

    public MyService(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public void DoSomething()
    {
        // Info (Blue)
        _notifications.ShowInfo("Game saved successfully.");

        // Success (Green)
        _notifications.ShowSuccess("Level Complete!");

        // Warning (Yellow)
        _notifications.ShowWarning("Low Health!");

        // Error (Red)
        _notifications.ShowError("Failed to connect to server.");

        // Custom
        _notifications.ShowMessage(
            "Custom Message",
            duration: 5.0f,
            textColor: Color4b.Black,
            backgroundColor: Color4b.White
        );
    }
}
```

### 2. Setup (If not using the default GameObjects plugin)

The `NotificationHudGameObject` is automatically registered if you use the `LillyGameObjectPlugin`. If you are manually composing scenes or not using the plugin, you must add the HUD object to your scene:

```csharp
public class MyScene : BaseScene
{
    public override void Initialize()
    {
        // Create the HUD. It will automatically resolve INotificationService
        // from the container and subscribe to events.
        var notificationHud = new NotificationHudGameObject();
        AddGameObject(notificationHud);
    }
}
```

## Notification Types

| Type | Default Color | Duration |
|------|---------------|----------|
| **Info** | Blue | 3s |
| **Success** | Green | 3s |
| **Warning** | Yellow | 4s |
| **Error** | Red | 5s |

## Scripting

Notifications are fully exposed to Lua via the `notifications` module.

```lua
-- scripts/game.lua

function on_level_complete()
    notifications.success("Level Complete!")
    notifications.info("Score: " .. player_score)
end

function on_error()
    notifications.error("Something went wrong!")
end
```

See `lua-scripting.md` for more details.
