using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Voxel.Plugin.Actionables;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Interfaces.Actionables;

namespace Lilly.Voxel.Plugin.Interfaces.Services;

public interface IActionableService
{
    void AddActionListener(IActionableListener listener);
    void Handle(ActionEventContext ctx);
    void OnPlace(Vector3 worldPos, ushort blockTypeId, BlockType type);
    void OnRemove(Vector3 worldPos);
    bool TryGetInstance(Vector3 worldPos, out BlockInstance inst);
    void Update(GameTime gameTime);
}
