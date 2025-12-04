using Lilly.Engine.Core.Extensions.Strings;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Builders;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Json.Entities;
using Serilog;

namespace Lilly.Voxel.Plugin.Services;

/// <summary>
/// Manages the registration and retrieval of block types in the voxel engine.
/// </summary>
public class BlockRegistry : IBlockRegistry
{
    private readonly ILogger _logger = Log.ForContext<BlockRegistry>();

    /// <summary>
    /// Gets the air block type, which represents empty space.
    /// </summary>
    public BlockType Air { get; }

    private ushort _nextId = 1; // Start IDs from 1, reserving 0 for "air" or "empty" blocks

    private readonly List<BlockType> _blocksById = new();
    private readonly Dictionary<string, BlockType> _blocksByName = new();

    /// <summary>
    /// Initializes a new instance of the BlockRegistry class.
    /// </summary>
    public BlockRegistry()
    {
        Air = new BlockType(0, "air");
        _blocksById.Add(Air); // ID 0 is Air
        _blocksByName["air"] = Air;
        Air.IsSolid = false;
        Air.IsTransparent = true;
        Air.IsBreakable = false;
    }

    /// <summary>
    /// Gets a block type by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the block type.</param>
    /// <returns>The block type, or Air if not found.</returns>
    public BlockType GetById(ushort id)
    {
        return id < _blocksById.Count ? _blocksById[id] : Air;
    }

    /// <summary>
    /// Gets a block type by its name.
    /// </summary>
    /// <param name="name">The name of the block type.</param>
    /// <returns>The block type, or Air if not found.</returns>
    public BlockType GetByName(string name)
    {
        return _blocksByName.GetValueOrDefault(name, Air);
    }

    /// <summary>
    /// Gets all registered block types.
    /// </summary>
    /// <returns>An enumerable of all block types.</returns>
    public IEnumerable<BlockType> GetAllBlocks()
        => _blocksById;

    /// <summary>
    /// Creates and registers a new block type with the specified name.
    /// </summary>
    /// <param name="name">The name of the new block type.</param>
    /// <returns>The newly created block type.</returns>
    public BlockType NewBlock(string name)
    {
        var nextId = _nextId++;
        name = name.ToSnakeCase();
        var block = new BlockType(nextId, name);

        if (_blocksById.Count == nextId)
        {
            _blocksById.Add(block);

        }
        else
        {
            // Fallback just in case IDs get out of sync, though shouldn't happen with sequential generation
            while (_blocksById.Count <= nextId)
            {
                _blocksById.Add(Air);
            }
            _blocksById[nextId] = block;
        }

        _blocksByName[name] = block;

        _logger.Information("Registered new block: {BlockName} with ID {BlockId}", name, nextId);

        return block;
    }

    /// <summary>
    /// Registers a new block type using a builder pattern.
    /// </summary>
    /// <param name="name">The name of the block type.</param>
    /// <param name="builder">An action to configure the block type builder.</param>
    public void RegisterBlock(string name, Action<BlockTypeBuilder> builder)
    {
        var block = NewBlock(name);
        var blockBuilder = new BlockTypeBuilder()
                           .WithId(block.Id)
                           .WithName(block.Name);

        builder(blockBuilder);

        var builtBlock = blockBuilder.Build();

        // Update the block in the registries
        _blocksById[block.Id] = builtBlock;
        _blocksByName[block.Name] = builtBlock;

    }

    public void RegisterBlockFromJson(BlockDefinitionJson blockJson)
    {
        RegisterBlock(
            blockJson.Name,
            builder =>
            {
                builder
                    .AsSolid(blockJson.IsSolid)
                    .AsLiquid(blockJson.IsLiquid)
                    .AsOpaque(blockJson.IsOpaque)
                    .AsTransparent(blockJson.IsTransparent)
                    .WithHardness(blockJson.Hardness)
                    .AsBreakable(blockJson.IsBreakable)
                    .AsBillboard(blockJson.IsBillboard)
                    .AsActionable(blockJson.IsItem);

                foreach (var face in blockJson.Faces)
                {
                    builder.WithTexture(face.Key, face.Value.AtlasName, face.Value.Index);
                }
            }
        );
    }
}
