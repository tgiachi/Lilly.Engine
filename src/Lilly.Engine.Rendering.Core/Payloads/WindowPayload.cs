using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Payloads;

public readonly struct WindowPayload
{
    public WindowSubCommandType SubCommandType { get; init; }
    public object? Data { get; init; }
}
