using Markdowser.Utilities;

using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Markdowser.Models;

public class Settings
{
    public static Settings Current { get; set; } = LoadSettings();

    public bool DarkMode { get; set; }

    public string SearchEngineUrl { get; set; } = "https://html.duckduckgo.com/html/?kd=-1&k1=-1&q={0}";

    public string? HomeUrl { get; set; }

    public string UserAgent { get; set; } = $"{Assembly.GetExecutingAssembly().GetName().Name}/{Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";

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
            try
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(Configuration.SettingsFilePath)) ?? new Settings();
            }
            catch (Exception ex)
            {
                GlobalState.Logger.LogWarn(ex.Message);
                return new Settings();
            }
        }
        else
        {
            return new Settings();
        }
    }
}