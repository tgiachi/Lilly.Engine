namespace Squid.Engine.World.Voxels.Noise;

/// <summary>
/// Defines the domain warp algorithms supported by <see cref="FastNoiseLite"/>.
/// </summary>
public enum DomainWarpType
{
    OpenSimplex2,
    OpenSimplex2Reduced,
    BasicGrid
}
