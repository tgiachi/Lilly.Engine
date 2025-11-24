using System.Reflection;
using Lilly.Engine.Core.Data.Internal.Version;
using Lilly.Engine.Core.Interfaces.Services;

namespace Lilly.Engine.Services;

/// <summary>
/// Implements the version service for retrieving application version information.
/// </summary>
public class VersionService : IVersionService
{
    /// <summary>
    /// Gets the version information.
    /// </summary>
    /// <returns>The version information.</returns>
    public VersionInfoData GetVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "0.0.0";
        var appName = assembly.GetName().Name;
        const string codeName = "Oceanus";

        return new(appName, codeName, version);
    }
}
