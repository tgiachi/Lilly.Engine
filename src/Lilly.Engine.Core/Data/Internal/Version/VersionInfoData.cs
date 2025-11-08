namespace Lilly.Engine.Core.Data.Internal.Version;

/// <summary>
/// Represents version information data.
/// </summary>
public record VersionInfoData(
    string AppName,
    string CodeName,
    string Version
);
