namespace Lilly.Engine.Rendering.Core.Data;

/// <summary>
/// Represents a graphics API version with major, minor, build, and revision numbers.
/// </summary>
/// <param name="Major">The major version number.</param>
/// <param name="Minor">The minor version number.</param>
/// <param name="Build">The build number.</param>
/// <param name="Revision">The revision number.</param>
public record GraphicApiVersion(int Major, int Minor, int Build, int Revision);

