using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Interfaces.Services;
using MoonSharp.Interpreter;
using Silk.NET.Input;

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
    [ScriptFunction("bindKey", "Binds a key to a callback action.")]
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
    [ScriptFunction("bindKeyContext", "Binds a key to a callback action with a specific context.")]
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
    [ScriptFunction("clearBindings", "Clears all key bindings.")]
    public void ClearBindings()
    {
        _inputManager.ClearBindings();
    }

    /// <summary>
    /// Gets the current input context name.
    /// </summary>
    /// <returns>The current context name as a string.</returns>
    [ScriptFunction("getContext", "Gets the current input context.")]
    public string GetContext()
        => _inputManager.CurrentContext;

    /// <summary>
    /// Checks if a key is currently down.
    /// </summary>
    /// <param name="keyName">The key name (e.g., "A", "Space", "Escape").</param>
    /// <returns>True if the key is down.</returns>
    [ScriptFunction("isKeyDown", "Checks if a key is currently down.")]
    public bool IsKeyDown(string keyName)
        => Enum.TryParse<Key>(keyName, true, out var key) && _inputManager.IsKeyDown(key);

    /// <summary>
    /// Checks if a key was just pressed this frame.
    /// </summary>
    /// <param name="keyName">The key name (e.g., "A", "Space", "Escape").</param>
    /// <returns>True if the key was just pressed.</returns>
    [ScriptFunction("isKeyPressed", "Checks if a key was just pressed.")]
    public bool IsKeyPressed(string keyName)
        => Enum.TryParse<Key>(keyName, true, out var key) &&
           _inputManager.IsKeyPressed(key);

    /// <summary>
    /// Sets the current input context.
    /// </summary>
    /// <param name="contextName">The context name (any string, e.g., "main_menu", "gameplay", "pause").</param>
    [ScriptFunction("setContext", "Sets the current input context.")]
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
    [ScriptFunction("unbindKey", "Unbinds a key.")]
    public void UnbindKey(string keyBinding)
    {
        _inputManager.UnbindKey(keyBinding);
    }
}
