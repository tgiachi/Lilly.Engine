using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Builders;
using Lilly.Voxel.Plugin.Json.Entities;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Interfaces.Services;

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
