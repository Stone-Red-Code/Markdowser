using System;
using System.IO;

namespace Markdowser.Utilities;

internal static class Configuration
{
    public static string DownloadPath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public static string ApplicationDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(Markdowser));

    public static string SettingsFilePath => Path.Combine(ApplicationDataPath, "settings.json");
}