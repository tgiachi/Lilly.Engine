using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Payloads;

/// <summary>
/// Payload data for window operation commands.
/// </summary>
public readonly struct WindowPayload
{
    public WindowSubCommandType SubCommandType { get; init; }
    public object? Data { get; init; }
}
