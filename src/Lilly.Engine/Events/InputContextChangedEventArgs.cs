namespace Lilly.Engine.Events;

/// <summary>
/// Event arguments for input context changes.
/// </summary>
public class InputContextChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous input context.
    /// </summary>
    public string OldContext { get; }

    /// <summary>
    /// Gets the new input context.
    /// </summary>
    public string NewContext { get; }

    /// <summary>
    /// Initializes a new instance of the InputContextChangedEventArgs class.
    /// </summary>
    /// <param name="oldContext">The previous context.</param>
    /// <param name="newContext">The new context.</param>
    public InputContextChangedEventArgs(string oldContext, string newContext)
    {
        OldContext = oldContext;
        NewContext = newContext;
    }
}
