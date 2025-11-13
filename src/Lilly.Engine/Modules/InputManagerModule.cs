using System.Globalization;
using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Types;
using MoonSharp.Interpreter;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Lilly.Engine.Modules;

/// <summary>
/// JavaScript module for input management.
/// </summary>
[ScriptModule("input_manager", "Input Manager Module")]

/// <summary>
/// Provides scripting access to input management, including key binding and context switching.
/// </summary>
public class InputManagerModule
{
    private readonly IInputManagerService _inputManager;

    /// <summary>
    /// Initializes a new instance of the InputManagerModule class.
    /// </summary>
    /// <param name="inputManager">The input manager service.</param>
    public InputManagerModule(IInputManagerService inputManager)
        => _inputManager = inputManager;

    /// <summary>
    /// Binds a key combination to a JavaScript callback function.
    /// </summary>
    /// <param name="keyBinding">The key binding string (e.g., "Ctrl+A", "F1", "Shift+Escape").</param>
    /// <param name="callback">The JavaScript function to execute when the key is pressed.</param>
    [ScriptFunction("bind_key", "Binds a key to a callback action.")]
    public void BindKey(string keyBinding, Closure callback)
    {
        _inputManager.BindKey(
            keyBinding,
            () =>
            {
                try
                {
                    callback.Call();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error executing key binding callback for '{keyBinding}': {ex.Message}",
                        ex
                    );
                }
            }
        );
    }

    /// <summary>
    /// Binds a key combination to a JavaScript callback function with a specific input context.
    /// </summary>
    /// <param name="keyBinding">The key binding string (e.g., "Ctrl+A", "F1", "Shift+Escape").</param>
    /// <param name="callback">The JavaScript function to execute when the key is pressed.</param>
    /// <param name="contextName">The input context name (any string, e.g., "main_menu", "gameplay", "pause").</param>
    [ScriptFunction("bind_key_context", "Binds a key to a callback action with a specific context.")]
    public void BindKeyWithContext(string keyBinding, Action callback, string contextName)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback), "Callback must be a function");
        }

        if (string.IsNullOrEmpty(contextName))
        {
            throw new ArgumentException(
                "Context name cannot be null or empty",
                nameof(contextName)
            );
        }

        _inputManager.BindKey(
            keyBinding,
            () =>
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error executing key binding callback for '{keyBinding}': {ex.Message}",
                        ex
                    );
                }
            },
            contextName
        );
    }

    /// <summary>
    /// Clears all key bindings.
    /// </summary>
    [ScriptFunction("clear_bindings", "Clears all key bindings.")]
    public void ClearBindings()
    {
        _inputManager.ClearBindings();
    }

    /// <summary>
    /// Gets the current input context name.
    /// </summary>
    /// <returns>The current context name as a string.</returns>
    [ScriptFunction("get_context", "Gets the current input context.")]
    public string GetContext()
        => _inputManager.CurrentContext;

    /// <summary>
    /// Checks if a key is currently down.
    /// </summary>
    /// <param name="keyName">The key name (e.g., "A", "Space", "Escape").</param>
    /// <returns>True if the key is down.</returns>
    [ScriptFunction("is_key_down", "Checks if a key is currently down.")]
    public bool IsKeyDown(string keyName)
        => Enum.TryParse<Key>(keyName, true, out var key) && _inputManager.IsKeyDown(key);

    /// <summary>
    /// Checks if a key was just pressed this frame.
    /// </summary>
    /// <param name="keyName">The key name (e.g., "A", "Space", "Escape").</param>
    /// <returns>True if the key was just pressed.</returns>
    [ScriptFunction("is_key_pressed", "Checks if a key was just pressed.")]
    public bool IsKeyPressed(string keyName)
        => Enum.TryParse<Key>(keyName, true, out var key) &&
           _inputManager.IsKeyPressed(key);

    /// <summary>
    /// Sets the current input context.
    /// </summary>
    /// <param name="contextName">The context name (any string, e.g., "main_menu", "gameplay", "pause").</param>
    [ScriptFunction("set_context", "Sets the current input context.")]
    public void SetContext(string contextName)
    {
        if (string.IsNullOrEmpty(contextName))
        {
            throw new ArgumentException(
                "Context name cannot be null or empty",
                nameof(contextName)
            );
        }

        _inputManager.CurrentContext = contextName;
    }

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="keyBinding">The key binding string to unbind.</param>
    [ScriptFunction("unbind_key", "Unbinds a key.")]
    public void UnbindKey(string keyBinding)
    {
        _inputManager.UnbindKey(keyBinding);
    }

    [ScriptFunction("grab_mouse", "Grabs the mouse cursor and makes it invisible.")]
    public void GrabMouse()
    {
        _inputManager.IsMouseVisible = false;
    }

    [ScriptFunction("show_mouse", "Shows the mouse cursor.")]
    public void ShowMouse()
    {
        _inputManager.IsMouseVisible = true;
    }

    [ScriptFunction("is_mouse_visible", "Checks if the mouse cursor is visible.")]
    public bool IsMouseVisible()
    {
        return _inputManager.IsMouseVisible;
    }

    [ScriptFunction("toggle_mouse", "Sets the mouse cursor visibility.")]
    public void ToggleMouseVisibility()
    {
        _inputManager.IsMouseVisible = !_inputManager.IsMouseVisible;
    }

    /// <summary>
    /// Gets the mouse delta (movement since last frame).
    /// </summary>
    /// <returns>A table with 'x' and 'y' keys representing the mouse delta.</returns>
    [ScriptFunction("get_mouse_delta", "Gets the mouse delta (movement since last frame).")]
    public DynValue GetMouseDelta()
    {
        var delta = _inputManager.GetMouseDelta();
        var table = new Table(null)
        {
            ["x"] = delta.X,
            ["y"] = delta.Y
        };

        return DynValue.NewTable(table);
    }

    /// <summary>
    /// Binds a callback to mouse movement globally (always active, regardless of context).
    /// Passes both delta (movement) and position (absolute coordinates).
    /// </summary>
    /// <param name="callback">The callback function to execute when mouse moves.</param>
    [ScriptFunction("bind_mouse", "Binds a callback to global mouse movement (always active).")]
    public void BindMouseMovement(Closure callback)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback), "Callback must be a function");
        }

        _inputManager.BindMouseMovement(
            (delta, position) =>
            {
                try
                {
                    callback.Call(delta.X, delta.Y, position.X, position.Y);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error executing global mouse movement callback: {ex.Message}",
                        ex
                    );
                }
            }
        );
    }

    /// <summary>
    /// Binds a callback to mouse movement, only active in a specific context.
    /// Passes both delta (movement) and position (absolute coordinates).
    /// </summary>
    /// <param name="callback">The callback function to execute when mouse moves.</param>
    /// <param name="contextName">The context in which this callback is active.</param>
    [ScriptFunction("bind_mouse_context", "Binds a callback to mouse movement for a specific context.")]
    public void BindMouseMovementContext(Closure callback, string contextName)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback), "Callback must be a function");
        }

        if (string.IsNullOrEmpty(contextName))
        {
            throw new ArgumentException("Context name cannot be null or empty", nameof(contextName));
        }

        _inputManager.BindMouseMovement(
            (delta, position) =>
            {
                try
                {
                    callback.Call(delta.X, delta.Y, position.X, position.Y);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error executing mouse movement callback for context '{contextName}': {ex.Message}",
                        ex
                    );
                }
            },
            contextName
        );
    }

    [ScriptFunction("convert_mouse_delta_to_pitch_yaw_roll", "Converts mouse delta to pitch, yaw, and roll values.")]
    public Table ConvertMouseDeltaToPitchYawRoll(int deltaX, int deltaY, float roll = 0f, float sensitivity = 0.3f)
    {
        if (sensitivity == 0)
        {
            sensitivity = 0.3f;
        }

        float pitch = deltaY * sensitivity;
        float yaw = deltaX * sensitivity;

        var table = new Table(null)
        {
            ["pitch"] = pitch,
            ["yaw"] = yaw,
            ["roll"] = roll
        };

        return table;
    }

    /// <summary>
    /// Binds a callback to mouse click events globally (always active, regardless of context).
    /// Passes the mouse button name (as string) and position (absolute coordinates).
    /// </summary>
    /// <param name="callback">The callback function to execute when mouse button is clicked.</param>
    [ScriptFunction("bind_mouse_click", "Binds a callback to global mouse click events (always active).")]
    public void BindMouseClick(Closure callback)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback), "Callback must be a function");
        }

        _inputManager.BindMouseClick(
            (button, position) =>
            {
                try
                {
                    var buttonName = button.ToString().ToLower(CultureInfo.InvariantCulture);
                    callback.Call(buttonName, position.X, position.Y);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error executing global mouse click callback: {ex.Message}",
                        ex
                    );
                }
            }
        );
    }

    /// <summary>
    /// Binds a callback to mouse click events, only active in a specific context.
    /// Passes the mouse button name (as string) and position (absolute coordinates).
    /// </summary>
    /// <param name="callback">The callback function to execute when mouse button is clicked.</param>
    /// <param name="contextName">The context in which this callback is active.</param>
    [ScriptFunction("bind_mouse_click_context", "Binds a callback to mouse click events for a specific context.")]
    public void BindMouseClickContext(Closure callback, string contextName)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback), "Callback must be a function");
        }

        if (string.IsNullOrEmpty(contextName))
        {
            throw new ArgumentException("Context name cannot be null or empty", nameof(contextName));
        }

        _inputManager.BindMouseClick(
            (button, position) =>
            {
                try
                {
                    var buttonName = button.ToString().ToLower(CultureInfo.InvariantCulture);
                    callback.Call(buttonName, position.X, position.Y);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error executing mouse click callback for context '{contextName}': {ex.Message}",
                        ex
                    );
                }
            },
            contextName
        );
    }
}
