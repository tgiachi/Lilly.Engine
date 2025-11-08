using System.Runtime.InteropServices;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Commands;

[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderCommand
{
    public RenderCommandType CommandType { get; init; }

    public object? Data { get; init; }

    public RenderCommand(RenderCommandType commandType, object? data = null)
    {
        CommandType = commandType;
        Data = data;
    }

    public TPayload GetPayload<TPayload>() where TPayload : struct
    {
        return Data is TPayload ? (TPayload)Data : default;
    }
}
