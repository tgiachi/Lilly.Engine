using System.Text.Json.Serialization;
using Lilly.Engine.Json.Assets;
using Lilly.Engine.Json.Materials;

namespace Lilly.Engine.Json.Context;

[JsonSerializable(typeof(LillyEngineAssetJson)),
 JsonSerializable(typeof(LillyEngineAssetEntryJson[])),
 JsonSerializable(typeof(LillyEngineAssetEntryJson)),
 JsonSerializable(typeof(LillyMaterialJson)),
 JsonSerializable(typeof(LillyMaterialJson[])),
 JsonSerializable(typeof(LillyAssetManifestJson))
]
public partial class LillyEngineJsonContext : JsonSerializerContext { }
