using Lilly.Engine.Rendering.Core.Data.Input;
using Serilog;

namespace Lilly.Engine.Services.Input;

/// <summary>
/// Handles key binding parsing and management.
/// </summary>
internal class BindingManager
{
    private readonly ILogger _logger = Log.ForContext<BindingManager>();

    private readonly KeyboardInputHandler _keyboardHandler;

    public BindingManager(KeyboardInputHandler keyboardHandler)
    {
        _keyboardHandler = keyboardHandler;
    }

    /// <summary>
    /// Binds a key combination to an action.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKey(string binding, Action action, string? context = null)
    {
        if (!KeyBinding.TryParse(binding, out var keyBinding))
        {
            _logger.Warning("Failed to parse key binding: {Binding}", binding);
            return;
        }

        _keyboardHandler.BindKey(keyBinding, action, context);
    }

    /// <summary>
    /// Binds a key combination to an action that executes every frame while held.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKeyHeld(string binding, Action action, string? context = null)
    {
        if (!KeyBinding.TryParse(binding, out var keyBinding))
        {
            _logger.Warning("Failed to parse key binding: {Binding}", binding);
            return;
        }

        _keyboardHandler.BindKeyHeld(keyBinding, action, context);
    }

    /// <summary>
    /// Binds a key combination to an action that executes with key repeat (initial press + delay + interval).
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKeyRepeat(string binding, Action action, string? context = null)
    {
        if (!KeyBinding.TryParse(binding, out var keyBinding))
        {
            _logger.Warning("Failed to parse key binding: {Binding}", binding);
            return;
        }

        _keyboardHandler.BindKeyRepeat(keyBinding, action, context);
    }

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    public void UnbindKey(string binding)
    {
        if (!KeyBinding.TryParse(binding, out var keyBinding))
        {
            _logger.Warning("Failed to parse key binding: {Binding}", binding);
            return;
        }

        _keyboardHandler.UnbindKey(keyBinding);
    }

    /// <summary>
    /// Unbinds a held key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    public void UnbindKeyHeld(string binding)
    {
        if (!KeyBinding.TryParse(binding, out var keyBinding))
        {
            _logger.Warning("Failed to parse key binding: {Binding}", binding);
            return;
        }

        _keyboardHandler.UnbindKeyHeld(keyBinding);
    }

    /// <summary>
    /// Unbinds a repeat key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    public void UnbindKeyRepeat(string binding)
    {
        if (!KeyBinding.TryParse(binding, out var keyBinding))
        {
            _logger.Warning("Failed to parse key binding: {Binding}", binding);
            return;
        }

        _keyboardHandler.UnbindKeyRepeat(keyBinding);
    }

    /// <summary>
    /// Clears all key bindings.
    /// </summary>
    public void ClearBindings()
    {
        _keyboardHandler.ClearBindings();
    }
}