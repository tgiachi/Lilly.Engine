namespace Squid.Engine.World.Voxels.Primitives;

/// <summary>
/// Represents a texture object with an atlas name and index for block rendering.
/// </summary>
public record struct BlockTextureObject(string AtlasName, int Index);

