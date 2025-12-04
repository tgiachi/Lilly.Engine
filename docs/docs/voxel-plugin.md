# Lilly.Voxel.Plugin

The `Lilly.Voxel.Plugin` is a powerful extension that transforms Lilly.Engine into a capable voxel game engine. It handles everything from chunk generation and mesh building to lighting propagation and custom block definitions.

## Features

- **Infinite Voxel World**: Procedurally generated terrain with chunks (16x256x16).
- **Pipeline Generation**: Customize how the world is built (heightmap → erosion → caves → decoration).
- **Lighting Engine**: 3D cellular automata-based light propagation (sunlight + block light).
- **Optimized Rendering**: Greedy meshing (planned), face culling, and shader-based effects.
- **Custom Blocks**: JSON-based block definitions with multi-face texturing and physical properties.

## Architecture

The plugin is built around a few key services:

### 1. Block Registry (`IBlockRegistry`)
Manages all available block types (`BlockType`). It loads definitions from JSON or code and maps them to numeric IDs.

### 2. Chunk Generator (`IChunkGeneratorService`)
The heart of the procedural generation. It uses a **Pipeline** pattern where you inject distinct "steps" to build a chunk.

**Standard Pipeline:**
1. **HeightMapGenerationStep**: Uses Perlin noise to determine base terrain height.
2. **TerrainErosionGenerationStep**: Simulates weathering (optional).
3. **TerrainFillGenerationStep**: Fills the chunk with blocks based on the heightmap (Stone, Dirt, Grass).
4. **CaveGenerationStep**: Carves out tunnels using 3D noise.
5. **SurfacePaintingStep**: Replaces top blocks with biome-specific blocks (Sand, Snow).
6. **DecorationGenerationStep**: Places trees, flowers, and structures.
7. **LightingGenerationStep**: Calculates initial light values.

### 3. Chunk Mesh Builder (`ChunkMeshBuilder`)
Converts the voxel data into OpenGL vertex buffers. It handles:
- **Face Culling**: Don't draw faces touching other opaque blocks.
- **AO (Ambient Occlusion)**: Calculates per-vertex lighting for depth.
- **Texture Mapping**: Assigns UV coordinates from the texture atlas.

## Customization

### Defining Blocks
Create a `blocks.json` file in your data folder:

```json
[
  {
    "name": "diamond_ore",
    "isSolid": true,
    "hardness": 5.0,
    "faces": {
      "All": "blocks@12"
    }
  },
  {
    "name": "lantern",
    "isSolid": true,
    "isLightSource": true,
    "emitsLight": 14,
    "faces": { "All": "blocks@50" }
  }
]
```

### Custom Generation Step
Implement `IGeneratorStep` to add your own logic (e.g., generating cities):

```csharp
public class CityGenerationStep : IGeneratorStep
{
    public string Name => "CityGeneration";

    public Task ExecuteAsync(GeneratorContext context)
    {
        // Check if this chunk should have a city
        if (ShouldSpawnCity(context.Position))
        {
            // Place blocks
            context.Chunk.SetBlock(x, y, z, cityBlockId);
        }
        return Task.CompletedTask;
    }
}
```

Register it in your plugin or startup code:

```csharp
var generator = container.Resolve<IChunkGeneratorService>();
generator.AddGeneratorStep(new CityGenerationStep());
```

## Usage

Enable the plugin in `Program.cs`:

```csharp
container.RegisterPlugin(typeof(LillyVoxelPlugin).Assembly);
```

Then create a world object in your scene:

```csharp
public override void Initialize()
{
    // Create world with seed
    var settings = new WorldGenerationSettings { Seed = 12345 };
    var world = new WorldGameObject(settings);
    AddGameObject(world);
}
```

## Scripting

The plugin exposes modules to Lua for runtime modification:

- `world`: Interact with the voxel grid (get/set blocks, raycast).
- `block_registry`: Register new blocks dynamically.
- `generation`: Configure the pipeline (limited support).

See [Building a Voxel World](tutorials/voxel-world.md) for a hands-on guide.
