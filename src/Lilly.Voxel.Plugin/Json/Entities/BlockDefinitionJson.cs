using System.Text.Json.Serialization;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Interfaces.Actionables;
using Lilly.Voxel.Plugin.Json.Converters;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Types;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Json.Entities;

public class BlockDefinitionJson
{
    public string Name { get; set; }

    public bool IsSolid { get; set; }

    public bool IsLiquid { get; set; }

    public bool IsOpaque { get; set; }

    public bool IsTransparent { get; set; }

    public float Hardness { get; set; }

    public bool IsBreakable { get; set; }

    public bool IsBillboard { get; set; }

    public bool IsItem { get; set; }


    [JsonConverter(typeof(HexColorConverter))]
    public Color4b EmitColor { get; set; } = Color4b.Transparent;

    public IActionableComponent[] Components { get; set; } = [];

    /// <summary>
    ///  Gets or sets the texture objects for each face of the block.
    ///  The values are serialized as strings in the format "atlasName@index".
    ///  Supports "all" key to set default texture for all faces, then override specific faces.
    ///  Ex. { "all": "default@2", "top": "grass@0", "bottom": "dirt@1" }
    /// </summary>
    [JsonConverter(typeof(BlockFaceDictionaryConverter))]
    public Dictionary<BlockFace, BlockTextureObject> Faces { get; set; }

    public override string ToString()
    {
        return
            $"BlockDefinitionJson(Name={Name}, IsSolid={IsSolid}, IsLiquid={IsLiquid}, IsOpaque={IsOpaque}, IsTransparent={IsTransparent}, Hardness={Hardness}, IsBreakable={IsBreakable}, IsBillboard={IsBillboard}, IsItem={IsItem}, Faces=[{string.Join(", ", Faces)}])";
    }
}
