namespace Squid.Engine.World.Voxels.Noise;

/// <summary>
/// Available return values when sampling cellular noise.
/// </summary>
public enum CellularReturnType
{
    CellValue,
    Distance,
    Distance2,
    Distance2Add,
    Distance2Sub,
    Distance2Mul,
    Distance2Div
}
