using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using DirectionalLight = Lilly.Rendering.Core.Lights.DirectionalLight;

namespace Lilly.Engine.GameObjects.ThreeD.Lights;

/// <summary>
/// GameObject wrapper for a directional light, syncing direction from transform rotation.
/// </summary>
public sealed class DirectionalLightGameObject : Base3dGameObject, IInitializable
{
    public DirectionalLight Light { get; } = new();

    /// <summary>
    /// If true, updates light direction from transform rotation (-Z).
    /// </summary>
    public bool SyncDirectionFromTransform { get; set; } = true;

    public DirectionalLightGameObject(IGameObjectManager gameObjectManager) : base("DirectionalLight", gameObjectManager) { }

    public void Initialize()
    {
        UpdateDirection();
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsActive || !SyncDirectionFromTransform)
        {
            return;
        }

        UpdateDirection();
    }

    private void UpdateDirection()
    {
        // Use -Z as forward in local space.
        var forward = Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, Transform.Rotation));
        Light.Direction = forward;
    }
}
