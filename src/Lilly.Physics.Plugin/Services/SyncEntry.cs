using BepuPhysics;
using Lilly.Engine.Data.Physics;
using Lilly.Rendering.Core.Primitives;

namespace Lilly.Physics.Plugin.Services;

internal readonly record struct SyncEntry(
    int BodyId,
    BodyHandle BodyHandle,
    Transform3D Transform,
    PhysicsSyncMode SyncMode
);
