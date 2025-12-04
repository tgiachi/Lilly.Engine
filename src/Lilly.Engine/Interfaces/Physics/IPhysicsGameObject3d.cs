using Lilly.Engine.Data.Physics;

namespace Lilly.Engine.Interfaces.Physics;

public interface IPhysicsGameObject3d
{
    bool IsStatic { get; }

    PhysicsBodyConfig BuildBodyConfig();

    void OnPhysicsAttached(IPhysicsBodyHandle h);
}
