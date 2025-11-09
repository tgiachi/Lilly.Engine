using Lilly.Engine.Types;

namespace Lilly.Engine.Data.Payloads;

public readonly struct WindowPayload
{
    public WindowSubCommandType SubCommandType { get; init; }
    public object? Data { get; init; }
}
