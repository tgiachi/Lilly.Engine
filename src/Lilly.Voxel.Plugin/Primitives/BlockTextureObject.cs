namespace Lilly.Voxel.Plugin.Primitives;

/// <summary>
/// Represents a texture object with an atlas name and index for block rendering.
/// </summary>
public record struct BlockTextureObject(string AtlasName, int Index);

