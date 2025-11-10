namespace Lilly.Engine.Core.Data.Internal.Version;

/// <summary>
/// Represents version information data.
/// </summary>
    /// <summary>
    /// Initializes a new instance of the VersionInfoData record.
    /// </summary>
    /// <param name="AppName">The application name.</param>
    /// <param name="CodeName">The code name.</param>
    /// <param name="Version">The version string.</param>
public record VersionInfoData(
    string AppName,
    string CodeName,
    string Version
);
