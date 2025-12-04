using System.Text.Json.Serialization;
using Lilly.Voxel.Plugin.Actionables.Components;
using Lilly.Voxel.Plugin.Json.Entities;

namespace Lilly.Voxel.Plugin.Json.Contexts;

[JsonSerializable(typeof(BlockDefinitionJson)),JsonSerializable(typeof(BlockDefinitionJson[]))]
public partial class LillyVoxelJsonContext : JsonSerializerContext { }
