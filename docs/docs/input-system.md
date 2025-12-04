# Input System

The Input System in Lilly.Engine provides a unified way to handle keyboard, mouse, and game input. It supports direct polling, event-based handling, and context-aware bindings.

## Overview

The `IInputManagerService` handles:

- **Direct Polling**: Checking the current state of keys and buttons.
- **Action Bindings**: Mapping key combinations to specific callbacks.
- **Input Contexts**: Grouping bindings (e.g., "Menu", "Gameplay") to switch controls easily.
- **Focus Management**: Routing input to specific UI elements or game objects.

## Basic Usage

### Direct Polling

Useful for continuous actions like movement in an `Update` loop.

```csharp
public override void Update(float deltaTime)
{
    // Check if key is currently held down
    if (_input.IsKeyDown(Key.W))
    {
        MovePlayer(Vector3.Forward);
    }

    // Check for single press (trigger)
    if (_input.IsKeyPressed(Key.Space))
    {
        Jump();
    }

    // Check for mouse button
    if (_input.IsMouseButtonPressed(MouseButton.Left))
    {
        FireWeapon();
    }
    
    // Get mouse movement delta
    var delta = _input.GetMouseDelta();
    RotateCamera(delta.X, delta.Y);
}
```

### Action Bindings

Bindings allow you to decouple input keys from logic. You define *what* happens, not *which key* does it.

```csharp
public override void Initialize()
{
    // Simple key binding
    _input.BindKey("Space", Jump);
    
    // Binding with modifier (e.g., Ctrl+S)
    _input.BindKey("ControlLeft+S", SaveGame);
    
    // Held binding (runs every frame)
    _input.BindKeyHeld("W", MoveForward);
}
```

## Input Contexts

Contexts let you have different control schemes active at different times (e.g., moving in game vs navigating a menu).

```csharp
// Define "Gameplay" bindings
_input.BindKey("Space", Jump, context: "Gameplay");
_input.BindKey("Escape", PauseGame, context: "Gameplay");

// Define "Menu" bindings
_input.BindKey("Escape", ResumeGame, context: "Menu");
_input.BindKey("Enter", SelectItem, context: "Menu");

// Switch context
_input.CurrentContext = "Gameplay"; 
// Now Space jumps, Escape pauses. Enter does nothing.

_input.CurrentContext = "Menu";
// Now Escape resumes, Enter selects. Space does nothing.
```

## Mouse Input

### Mouse Visibility

```csharp
// Hide cursor for FPS games
_input.IsMouseVisible = false;

// Show cursor for menus
_input.IsMouseVisible = true;
```

### Mouse Bindings

```csharp
// Handle mouse movement
_input.BindMouseMovement((delta, pos) => 
{
    Camera.Rotate(delta.X, delta.Y);
}, context: "Gameplay");

// Handle clicks
_input.BindMouseClick((button, pos) =>
{
    if (button == MouseButton.Left) Shoot();
}, context: "Gameplay");
```

## Focus Management

The system supports a stack-based focus mechanism, primarily for UI.

```csharp
// Give focus to a text box
_input.SetFocus(myTextBox);

// Push a modal dialog to focus stack (steals all input)
_input.PushFocusStack(myDialog);

// Restore previous focus
_input.PopFocusStack();
```

Objects implementing `IInputReceiver` can handle input events directly when focused.

## Scripting

Input is fully exposed to Lua via the `input_manager` module.

```lua
-- Simple binding
input_manager.bind_key("Space", function() 
    console.log("Jump!") 
end)

-- Contexts
input_manager.set_context("Menu")
```

See `lua-scripting.md` for the full API.
