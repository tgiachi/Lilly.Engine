using System.Text.Json.Serialization;
using Lilly.Voxel.Plugin.Actionables.Components;
using Lilly.Voxel.Plugin.Json.Entities;

namespace Lilly.Voxel.Plugin.Json.Contexts;

[
    JsonSerializable(typeof(BlockDefinitionJson)), JsonSerializable(typeof(BlockDefinitionJson[])),
    JsonSerializable(typeof(SoundComponent)), JsonSerializable(typeof(SoundComponent[])),
    JsonSerializable(typeof(NotificationComponent)), JsonSerializable(typeof(NotificationComponent[]))
]
public partial class LillyVoxelJsonContext : JsonSerializerContext { }
