using Lilly.Engine.Core.Enums;

namespace Lilly.Engine.Json.Assets;

public class LillyEngineAssetEntryJson
{
    public string Name { get; set; }
    public string Path { get; set; }
    public AssetType Type { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();

    public override string ToString()
        => $"{Name} ({Path})";
}
