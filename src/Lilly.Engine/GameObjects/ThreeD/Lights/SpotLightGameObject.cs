using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using SpotLight = Lilly.Rendering.Core.Lights.SpotLight;

namespace Lilly.Engine.GameObjects.ThreeD.Lights;

/// <summary>
/// GameObject wrapper for a spot light, syncing position and direction from transform.
/// </summary>
public sealed class SpotLightGameObject : Base3dGameObject, IInitializable
{
    public SpotLight Light { get; } = new();

    /// <summary>
    /// If true, updates position from transform.
    /// </summary>
    public bool SyncPositionFromTransform { get; set; } = true;

    /// <summary>
    /// If true, updates direction from transform rotation (-Z).
    /// </summary>
    public bool SyncDirectionFromTransform { get; set; } = true;

    public SpotLightGameObject(IGameObjectManager gameObjectManager) : base("SpotLight", gameObjectManager)
    {
    }

    public void Initialize()
    {
        UpdateTransforms();
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }

        UpdateTransforms();
    }

    private void UpdateTransforms()
    {
        if (SyncPositionFromTransform)
        {
            Light.Position = Transform.Position;
        }

        if (SyncDirectionFromTransform)
        {
            var forward = Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, Transform.Rotation));
            Light.Direction = forward;
        }
    }
}
