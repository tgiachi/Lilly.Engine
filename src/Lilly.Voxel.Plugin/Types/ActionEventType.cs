namespace Lilly.Voxel.Plugin.Types;

[Flags]
public enum ActionEventType : byte
{
    OnPlace,
    OnBreak,
    OnUse,
    OnTick,
    OnNeighborChange,
    All = OnPlace | OnBreak | OnUse | OnTick | OnNeighborChange
}
