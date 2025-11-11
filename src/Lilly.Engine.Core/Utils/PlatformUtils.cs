using Lilly.Engine.Core.Enums;

namespace Lilly.Engine.Core.Utils;

public static class PlatformUtils
{
    public static bool IsRunningOnMacOS()
    {
        return OperatingSystem.IsMacOS();
    }

    public static bool IsRunningOnWindows()
    {
        return OperatingSystem.IsWindows();
    }

    public static bool IsRunningOnLinux()
    {
        return OperatingSystem.IsLinux();
    }


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

}
