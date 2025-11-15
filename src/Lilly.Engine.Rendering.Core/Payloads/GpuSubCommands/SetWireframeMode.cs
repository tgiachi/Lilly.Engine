namespace Lilly.Engine.Rendering.Core.Payloads.GpuSubCommands;


public readonly struct SetWireframeMode
{
    public bool Enabled { get; init; }

    public SetWireframeMode(bool enabled)
    {
        Enabled = enabled;
    }

}
