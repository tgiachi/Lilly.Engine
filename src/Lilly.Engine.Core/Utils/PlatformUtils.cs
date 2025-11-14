using Lilly.Engine.Core.Enums;

namespace Lilly.Engine.Core.Utils;

public static class PlatformUtils
{
    public static PlatformType GetCurrentPlatform()
    {
        if (IsRunningOnWindows())
        {
            return PlatformType.Windows;
        }

        if (IsRunningOnMacOS())
        {
            return PlatformType.MacOS;
        }

        return IsRunningOnLinux() ? PlatformType.Linux : PlatformType.Unknown;
    }

    public static bool IsRunningOnLinux()
        => OperatingSystem.IsLinux();

    public static bool IsRunningOnMacOS()
        => OperatingSystem.IsMacOS();

    public static bool IsRunningOnWindows()
        => OperatingSystem.IsWindows();
}
