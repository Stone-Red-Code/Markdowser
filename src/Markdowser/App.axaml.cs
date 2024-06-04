using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

using CuteUtils.Logging;

using Markdowser.Models;
using Markdowser.Utilities;
using Markdowser.ViewModels;
using Markdowser.Views;

using System.IO;

namespace Markdowser;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (!Directory.Exists(Configuration.ApplicationDataPath))
        {
            _ = Directory.CreateDirectory(Configuration.ApplicationDataPath);
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            // Clear log files
            GlobalState.Logger.ClearLogFile(LogSeverity.Debug);
            GlobalState.Logger.ClearLogFile(LogSeverity.Info);
            GlobalState.Logger.ClearLogFile(LogSeverity.Warn);
            GlobalState.Logger.ClearLogFile(LogSeverity.Error);
            GlobalState.Logger.ClearLogFile(LogSeverity.Fatal);

            desktop.MainWindow.Closing += (sender, e) =>
            {
                GlobalState.Logger.LogInfo("Saving settings...");
                Settings.SaveSettings();
                GlobalState.Logger.LogInfo("Settings saved.");
            };
        }

        GlobalState.Logger.LogInfo("Application started.");

        base.OnFrameworkInitializationCompleted();

        if (Current is not null)
        {
            Current.RequestedThemeVariant = Settings.Current.DarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}