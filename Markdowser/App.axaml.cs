using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

using Markdowser.Models;
using Markdowser.ViewModels;
using Markdowser.Views;

namespace Markdowser;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            desktop.MainWindow.Closing += (sender, e) =>
            {
                Settings.SaveSettings();
            };
        }

        base.OnFrameworkInitializationCompleted();

        if (Current is not null)
        {
            Current.RequestedThemeVariant = Settings.Current.DarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}