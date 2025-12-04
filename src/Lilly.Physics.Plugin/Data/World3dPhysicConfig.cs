using System.Numerics;

namespace Lilly.Physics.Plugin.Data;

public class World3dPhysicConfig
{
    public Vector3 Gravity { get; set; } = new Vector3(0, -9.81f, 0);

    public int ThreadCount { get; set; } = 2;
}
