using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using PointLight = Lilly.Rendering.Core.Lights.PointLight;

namespace Lilly.Engine.GameObjects.ThreeD.Lights;

/// <summary>
/// GameObject wrapper for a point light, syncing position from transform.
/// </summary>
public sealed class PointLightGameObject : Base3dGameObject, IInitializable
{
    public PointLight Light { get; } = new();

    /// <summary>
    /// If true, updates light position from transform.
    /// </summary>
    public bool SyncPositionFromTransform { get; set; } = true;

    public PointLightGameObject(IGameObjectManager gameObjectManager) : base("PointLight", gameObjectManager)
    {
    }

    public void Initialize()
    {
        UpdatePosition();
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsActive || !SyncPositionFromTransform)
        {
            return;
        }

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        Light.Position = Transform.Position;
    }
}
