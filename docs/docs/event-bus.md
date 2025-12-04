# Event Bus System

The Event Bus provides a loosely coupled communication mechanism between different parts of the engine. It allows systems to publish events without knowing who subscribers are, and subscribers to react to events without knowing the source.

## Overview

The `IEventBusService` supports:

- **Asynchronous Publishing**: Fire an event and let subscribers handle it in the background (via a Channel).
- **Synchronous Publishing**: Fire an event and wait for all subscribers to finish immediately.
- **Strongly Typed Events**: Use C# classes/structs as event payloads.

## Usage

### 1. Define an Event

Events are simple POCOs (Plain Old C# Objects).

```csharp
public class PlayerDiedEvent
{
    public int PlayerId { get; set; }
    public Vector3 DeathPosition { get; set; }
}

public record GamePausedEvent(bool IsPaused);
```

### 2. Subscribe to Events

Inject `IEventBusService` and subscribe.

**Using a Lambda/Function:**

```csharp
public class GameManager : IDisposable
{
    private readonly IDisposable _subscription;

    public GameManager(IEventBusService eventBus)
    {
        _subscription = eventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    private Task OnPlayerDied(PlayerDiedEvent evt, CancellationToken token)
    {
        Console.WriteLine($"Player {evt.PlayerId} died!");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _subscription.Dispose(); // Unsubscribe
    }
}
```

**Using an Interface (`IEventBusListener<T>`):**

```csharp
public class AchievementSystem : IEventBusListener<PlayerDiedEvent>
{
    public Task HandleAsync(PlayerDiedEvent evt, CancellationToken token)
    {
        // Check for "First Death" achievement
        return Task.CompletedTask;
    }
}

// Registration
eventBus.Subscribe(new AchievementSystem());
```

### 3. Publish Events

**Async (Recommended):**
Queues the event to be processed by background workers. Does not block the main game loop.

```csharp
await _eventBus.PublishAsync(new PlayerDiedEvent { PlayerId = 1 });
```

**Immediate:**
Blocks execution until all handlers complete. Useful for critical sequence-dependent logic.

```csharp
await _eventBus.PublishImmediateAsync(new GamePausedEvent(true));
```

## Best Practices

1.  **Keep Payloads Small**: Events should carry data, not heavy logic.
2.  **Avoid Blocking Handlers**: If using `PublishAsync`, handlers run on a background thread. If using `PublishImmediateAsync`, a slow handler will freeze the game.
3.  **Unsubscribe**: Always dispose of your subscriptions to prevent memory leaks, especially in dynamic objects like GameObjects or Scenes.
4.  **Thread Safety**: Handlers may run concurrently or on background threads. Access shared resources carefully (e.g. use `JobSystem.RunInMainThread` if touching the UI).
