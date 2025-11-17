namespace Squid.Engine.World.Voxels.Noise;

/// <summary>
/// Fractal variations available for noise generation.
/// </summary>
public enum FractalType
{
    None,
    FBm,
    Ridged,
    PingPong,
    DomainWarpProgressive,
    DomainWarpIndependent
}
