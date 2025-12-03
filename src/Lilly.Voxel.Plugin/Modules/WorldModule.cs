using DryIoc;
using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.GameObjects;

namespace Lilly.Voxel.Plugin.Modules;

[ScriptModule("world", "Provides access to world-related functionalities.")]
public class WorldModule
{
    private readonly IContainer _container;

    public WorldModule(IContainer container)
    {
        _container = container;
    }

    [ScriptFunction("remove_block", "Removes the block at the current targeted position in the voxel world.")]
    public void RemoveBlock()
    {
        var gameObjectManager = _container.Resolve<IGameObjectManager>();

        var voxelGameObject = gameObjectManager.GetGameObjectOfType<WorldGameObject>();

        if (voxelGameObject == null)
        {
            throw new InvalidOperationException("No WorldGameObject found in the game object manager.");
        }

        //
        voxelGameObject.RemoveBlockAtCurrentPosition();
    }
}
