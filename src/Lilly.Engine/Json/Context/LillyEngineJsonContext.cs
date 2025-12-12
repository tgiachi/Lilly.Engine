using System.Text.Json.Serialization;
using Lilly.Engine.Json.Assets;

namespace Lilly.Engine.Json.Context;

[JsonSerializable(typeof(LillyEngineAssetJson)),
 JsonSerializable(typeof(LillyEngineAssetEntryJson[])),
 JsonSerializable(typeof(LillyEngineAssetEntryJson)),
]
public partial class LillyEngineJsonContext : JsonSerializerContext { }
