using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Builders;
using Lilly.Voxel.Plugin.Json.Entities;

namespace Lilly.Voxel.Plugin.Interfaces.Services;

public interface IBlockRegistry
{
    BlockType Air { get; }

    IEnumerable<BlockType> GetAllBlocks();

    BlockType GetById(ushort id);

    BlockType GetByName(string name);

    BlockType NewBlock(string name);

    void RegisterBlock(string name, Action<BlockTypeBuilder> builder);

    void RegisterBlockFromJson(BlockDefinitionJson blockJson);
}
