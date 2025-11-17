using Lilly.Voxel.Plugin.Builders;
using Lilly.Voxel.Plugin.Json.Entities;
using Lilly.Voxel.Plugin.Primitives;
using Squid.Engine.World.Voxels.Primitives;

namespace Squid.Engine.World.Voxels.Interfaces.Services;

public interface IBlockRegistry
{

    BlockType Air { get; }

    BlockType GetById(ushort id);

    BlockType GetByName(string name);

    IEnumerable<BlockType> GetAllBlocks();

    BlockType NewBlock(string name);

    void RegisterBlock(string name, Action<BlockTypeBuilder> builder);

    void RegisterBlockFromJson(BlockDefinitionJson blockJson);
}
