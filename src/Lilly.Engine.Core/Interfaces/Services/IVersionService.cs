using Lilly.Engine.Core.Data.Internal.Version;

namespace Lilly.Engine.Core.Interfaces.Services;

/// <summary>
/// Interface for the version service that provides version information.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Gets the version information.
    /// </summary>
    /// <returns>The version information.</returns>
    VersionInfoData GetVersionInfo();
}
