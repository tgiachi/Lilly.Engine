using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Enums;
using Lilly.Engine.Core.Json;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Json.Entities;
using MoonSharp.Interpreter;
using Serilog;

namespace Lilly.Voxel.Plugin.Modules;

[ScriptModule(
    "block_registry",
    "Provides access to the block registry, allowing retrieval of block types and their properties."
)]

/// <summary>
/// Provides scripting access to the block registry for registering and creating block types.
/// </summary>
public class BlockRegistryModule
{
    private readonly IBlockRegistry _blockRegistry;

    private readonly ILogger _logger = Log.ForContext<BlockRegistryModule>();
    private readonly DirectoriesConfig _directoriesConfig;

    public BlockRegistryModule(IBlockRegistry blockRegistry, DirectoriesConfig directoriesConfig)
    {
        _blockRegistry = blockRegistry;
        _directoriesConfig = directoriesConfig;
    }

    [ScriptFunction("load_blocks_from_data", "Loads block definitions from a JSON file in the data directory.")]
    public void LoadBlocksFromData(string fileName)
    {
        var filePath = Path.Combine(_directoriesConfig[DirectoryType.Data], fileName);

        if (File.Exists(filePath))
        {
            var blocks = JsonUtils.DeserializeFromFile<BlockDefinitionJson[]>(filePath);

            foreach (var blockJson in blocks)
            {
                _blockRegistry.RegisterBlockFromJson(blockJson);
            }

            _logger.Information("Loaded {Count} blocks from {FileName}", blocks.Length, fileName);

            return;
        }

        throw new FileNotFoundException($"The file {fileName} was not found.");
    }

    [ScriptFunction("new_block", "Creates a new block type with the specified name.")]
    public BlockType NewBlock(string name)
        => _blockRegistry.NewBlock(name);

    [ScriptFunction(
        "register_block",
        "Registers a new block type with the given name and properties defined in the builder closure."
    )]
    public void RegisterBlock(string name, Closure builder)
    {
        _blockRegistry.RegisterBlock(
            name,
            typeBuilder =>
            {
                builder.Call(typeBuilder);
            }
        );
    }
}
