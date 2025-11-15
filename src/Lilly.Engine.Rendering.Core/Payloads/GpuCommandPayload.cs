using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Payloads;

public readonly  struct  GpuCommandPayload
{
    public GpuSubCommandType CommandType { get; init;  }

    public object CommandPayload { get; init;  }

    public GpuCommandPayload(GpuSubCommandType commandType, object commandPayload)
    {
        CommandType = commandType;
        CommandPayload = commandPayload;
    }


    public T GetPayloadAs<T>() where T : struct
    {
        return (T)CommandPayload;
    }

}
