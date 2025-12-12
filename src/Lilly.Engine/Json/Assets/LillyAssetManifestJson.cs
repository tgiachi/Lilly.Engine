namespace Lilly.Engine.Json.Assets;

/// <summary>
/// Asset manifest used for bulk loading (e.g., from a zip). Mirrors the individual asset entries in a single JSON file.
/// </summary>
public class LillyAssetManifestJson
{
    public LillyEngineAssetEntryJson[] Assets { get; set; } = [];
    public string Version { get; set; } = "1.0.0";
}
