using System.IO;
using System.Text.Json;

namespace Markdowser.Utilities;

internal class Settings
{
    public static Settings Current { get; set; } = LoadSettings();

    public bool DarkMode { get; set; }

    public string? HomeUrl { get; set; }

    public static void SaveSettings()
    {
        if (!Directory.Exists(Configuration.ApplicationDataPath))
        {
            _ = Directory.CreateDirectory(Configuration.ApplicationDataPath);
        }

        File.WriteAllText(Configuration.SettingsFilePath, JsonSerializer.Serialize(Current));
    }

    private static Settings LoadSettings()
    {
        if (File.Exists(Configuration.SettingsFilePath))
        {
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(Configuration.SettingsFilePath)) ?? new Settings();
        }
        else
        {
            return new Settings();
        }
    }
}