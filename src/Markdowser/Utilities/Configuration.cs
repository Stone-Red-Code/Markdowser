using Syroot.Windows.IO;

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Markdowser.Utilities;

internal static class Configuration
{
    public static string DownloadPath
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return KnownFolders.Downloads.Path;
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads");
        }
    }

    public static string ApplicationDataPath
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StoneRed", nameof(Markdowser));
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StoneRed", nameof(Markdowser));
        }
    }

    public static string SettingsFilePath => Path.Combine(ApplicationDataPath, "settings.json");
    public static string LogFilePath => Path.Combine(ApplicationDataPath, $"{nameof(Markdowser)}.log");
}