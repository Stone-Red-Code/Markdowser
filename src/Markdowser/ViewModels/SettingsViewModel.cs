using Markdowser.Models;

namespace Markdowser.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    public string? HomeUrl
    {
        get => Settings.Current.HomeUrl;
        set => Settings.Current.HomeUrl = value;
    }

    public string SearchEngineUrl
    {
        get => Settings.Current.SearchEngineUrl;
        set => Settings.Current.SearchEngineUrl = value;
    }

    public bool DarkMode
    {
        get => Settings.Current.DarkMode;
        set => Settings.Current.DarkMode = value;
    }
}