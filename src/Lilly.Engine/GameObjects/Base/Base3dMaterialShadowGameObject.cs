using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Materials;

namespace Lilly.Engine.GameObjects.Base;

/// <summary>
/// Base 3D game object that supports materials and shadow casting/receiving.
/// </summary>
public abstract class Base3dMaterialShadowGameObject : Base3dShadowGameObject, IMaterialCaster
{
    public Material? Material { get; set; }

    protected Base3dMaterialShadowGameObject(string name, IGameObjectManager gameObjectManager, uint zIndex = 0)
        : base(name, gameObjectManager, zIndex)
    {
    }
}
