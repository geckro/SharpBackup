using System;

namespace SharpBackup.App.Common;

public static class PlatformUtils
{
    public static string GetPlatform()
    {
        return Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => "windows",
            PlatformID.MacOSX => "macos",
            PlatformID.Unix => "linux",
            _ => throw new Exception("Operating System not supported.")
        };
    }
}