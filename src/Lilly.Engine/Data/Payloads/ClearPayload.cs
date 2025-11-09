using TrippyGL;

namespace Lilly.Engine.Data.Payloads;

/// <summary>
/// Payload data for screen clear commands.
/// </summary>
public readonly struct ClearPayload
{
    /// <summary>
    /// Gets the color to clear the screen with.
    /// </summary>
    public Color4b Color { get; init; }

    /// <summary>
    /// Initializes a new instance of the ClearPayload struct.
    /// </summary>
    /// <param name="color">Optional clear color (defaults to black).</param>
    public ClearPayload(Color4b? color = null)
        => Color = color ?? Color4b.Black;
}
