using BepuPhysics;

namespace Lilly.Physics.Plugin.Services;

internal readonly record struct BodyRecord(bool IsStatic, BodyHandle BodyHandle, StaticHandle StaticHandle)
{
    public static BodyRecord Dynamic(BodyHandle bodyHandle)
        => new(false, bodyHandle, default);

    public static BodyRecord Static(StaticHandle staticHandle)
        => new(true, default, staticHandle);
}
