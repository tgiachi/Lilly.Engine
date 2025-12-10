using Lilly.Engine.Cameras;

namespace Lilly.Voxel.Plugin.Cameras;

public class PhysicFpsCamera : FPSCamera
{
    public delegate void MoveInFPSStyleDelegate(float forward, float right, float up, float deltaTime);

    public event MoveInFPSStyleDelegate? OnMoveInFPSStyle;
    public PhysicFpsCamera(string name) : base(name) { }

    public override void MoveInFPSStyle(float forward, float right, float up, float deltaTime)
    {
        OnMoveInFPSStyle?.Invoke(forward, right, up, deltaTime);

        //base.MoveInFPSStyle(forward, right, up, deltaTime);
    }
}
