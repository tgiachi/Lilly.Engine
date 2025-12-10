using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Physics;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Physics;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Primitives;
using Lilly.Voxel.Plugin.Cameras;

namespace Lilly.Voxel.Plugin.GameObjects;

public class PlayerGameObject : Base3dGameObject, IPhysicsGameObject3d
{
    private readonly ICamera3dService _camera3dService;
    private readonly IPhysicWorld3d _physicWorld3d;
    private readonly PhysicFpsCamera _playerCamera = new PhysicFpsCamera("PlayerCamera");
    private IPhysicsBodyHandle? _bodyHandle;
    private bool _jumpHeld;

    public float CapsuleRadius { get; set; } = 0.4f;
    public float CapsuleHeight { get; set; } = 1.8f;
    public float Mass { get; set; } = 1f;
    public float MoveImpulse { get; set; } = 12f;
    public float VerticalImpulse { get; set; } = 8f;
    public Vector3 CameraOffset { get; set; } = new(0f, 0.8f, 0f);

    public PlayerGameObject(
        IGameObjectManager gameObjectManager,
        ICamera3dService camera3dService,
        IPhysicWorld3d physicWorld3d
    ) : base(
        "PlayerGameObject",
        gameObjectManager
    )
    {
        _camera3dService = camera3dService;
        _physicWorld3d = physicWorld3d;
    }

    public bool IsStatic => false;
    public event Action? PhysicsShapeDirty;
    public Transform3D PhysicsTransform => Transform;

    public PhysicsBodyConfig BuildBodyConfig()
    {
        var radius = CapsuleRadius * MathF.Max(Transform.Scale.X, Transform.Scale.Z);
        var length = MathF.Max(0f, CapsuleHeight * Transform.Scale.Y - 2f * radius);
        var mass = MathF.Max(Mass, 0.001f);

        return new(
            new CapsuleShape(radius, length),
            mass,
            new(Transform.Position, Transform.Rotation)
        );
    }

    public void OnPhysicsAttached(IPhysicsBodyHandle h)
    {
        _bodyHandle = h;
    }

    public void OnPhysicsDetached()
    {
        _bodyHandle = null;
    }

    public override void Initialize()
    {
        _playerCamera.OnMoveInFPSStyle += PlayerCameraOnOnMoveInFPSStyle;
        _camera3dService.RegisterCamera(_playerCamera);

        if (_camera3dService.CurrentCamera == null)
        {
            _camera3dService.CurrentCamera = _playerCamera;
        }

        _playerCamera.Position = Transform.Position + CameraOffset;
        _playerCamera.Target = _playerCamera.Position + _playerCamera.Forward;

        base.Initialize();
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }

        _playerCamera.Position = Transform.Position + CameraOffset;
        _playerCamera.Target = _playerCamera.Position + _playerCamera.Forward;

        base.Update(gameTime);
    }

    private void PlayerCameraOnOnMoveInFPSStyle(float forward, float right, float up, float deltaTime)
    {
        if (_bodyHandle == null)
        {
            return;
        }

        var forwardFlat = new Vector3(_playerCamera.Forward.X, 0f, _playerCamera.Forward.Z);
        var rightFlat = new Vector3(_playerCamera.Right.X, 0f, _playerCamera.Right.Z);

        forwardFlat = forwardFlat.LengthSquared() > 1e-6f ? Vector3.Normalize(forwardFlat) : Vector3.Zero;

        rightFlat = rightFlat.LengthSquared() > 1e-6f ? Vector3.Normalize(rightFlat) : Vector3.Zero;

        var moveDirection = forwardFlat * forward + rightFlat * right;

        if (moveDirection.LengthSquared() > 1e-8f)
        {
            var impulse = moveDirection * MoveImpulse * deltaTime;
            _physicWorld3d.ApplyImpulse(_bodyHandle, impulse, Vector3.Zero);
        }

        if (up > 0.1f)
        {
            // Interpret up as jump: apply a single impulse on press, not every frame while held.
            if (!_jumpHeld)
            {
                var verticalImpulse = new Vector3(0f, VerticalImpulse, 0f);
                _physicWorld3d.ApplyImpulse(_bodyHandle, verticalImpulse, Vector3.Zero);
            }

            _jumpHeld = true;
        }
        else
        {
            _jumpHeld = false;
        }
    }
}
