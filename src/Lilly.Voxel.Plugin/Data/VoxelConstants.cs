namespace Lilly.Voxel.Plugin.Data;

public static class VoxelConstants
{
    /// <summary>
    /// The maximum light level a block can have (0-15 standard, but we limit to 13).
    /// </summary>
    public const byte MaxLightLevel = 10;

    /// <summary>
    /// The maximum light level as a float for calculations.
    /// </summary>
    public const float MaxLightLevelF = 10f;

    /// <summary>
    ///  The minimum light level a block can have.
    /// </summary>
    public const int MinLightLevel = 0;
}
