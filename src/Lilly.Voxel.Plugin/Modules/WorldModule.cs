using DryIoc;
using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Layers;
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
        var voxelGameObject = _container.Resolve<RenderLayerSystem3D>().GetGameObject<VoxelWorldGameObject>();

        voxelGameObject.RemoveCurrentBlock();
    }
}
