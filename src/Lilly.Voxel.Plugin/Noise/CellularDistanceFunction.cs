namespace Lilly.Voxel.Plugin.Noise;

/// <summary>
/// Supported distance functions for cellular noise evaluation.
/// </summary>
public enum CellularDistanceFunction
{
    Euclidean,
    EuclideanSq,
    Manhattan,
    Hybrid
}
