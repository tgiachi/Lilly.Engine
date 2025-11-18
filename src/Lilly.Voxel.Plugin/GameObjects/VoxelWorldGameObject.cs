using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.GameObjects.Environment;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

public class VoxelWorldGameObject : BaseGameObject3D
{
    private readonly RainEffectGameObject _rainEffect;
    private readonly SnowEffectGameObject _snowEffect;
    private readonly SkyGameObject _skyGameObject;

    public bool IsRaining
    {
        get => _rainEffect.IsVisible;
        set => _rainEffect.IsVisible = value;
    }

    public bool IsSnowing
    {
        get => _snowEffect.IsVisible;
        set => _snowEffect.IsVisible = value;
    }

    public VoxelWorldGameObject(IGameObjectFactory gameObjectFactory, GraphicsDevice graphicsDevice) : base(graphicsDevice)
    {
        IgnoreFrustumCulling = true;

        _rainEffect = gameObjectFactory.Create<RainEffectGameObject>();
        _snowEffect = gameObjectFactory.Create<SnowEffectGameObject>();
        _skyGameObject = gameObjectFactory.Create<SkyGameObject>();

        AddChild(_skyGameObject);
        AddChild(_rainEffect);
        AddChild(_snowEffect);

        IsRaining = false;
        IsSnowing = false;
    }

}
