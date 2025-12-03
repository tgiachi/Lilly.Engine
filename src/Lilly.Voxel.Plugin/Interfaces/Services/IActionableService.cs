using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Voxel.Plugin.Actionables;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Interfaces.Actionables;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Interfaces.Services;

public interface IActionableService
{
    bool TryGetInstance(Vector3 worldPos, out BlockInstance inst);
    void OnPlace(Vector3 worldPos, ushort blockTypeId, BlockType type);
    void OnRemove(Vector3 worldPos);
    void Handle(ActionEventContext ctx);
    void TickActive(GameTime gameTime);
    void AddActionListener(IActionableListener listener);
}
