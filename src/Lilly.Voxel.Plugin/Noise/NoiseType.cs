namespace Lilly.Voxel.Plugin.Noise;

/// <summary>
/// Fundamental noise algorithms exposed by <see cref="FastNoiseLite"/>.
/// </summary>
public enum NoiseType
{
    OpenSimplex2,
    OpenSimplex2S,
    Cellular,
    Perlin,
    ValueCubic,
    Value
}
