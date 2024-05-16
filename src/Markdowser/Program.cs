using Avalonia;
using Avalonia.ReactiveUI;

using Markdowser.Utilities;

using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Projektanker.Icons.Avalonia.MaterialDesign;

using ReactiveUI;

using System;
using System.Reactive;
using System.Threading.Tasks;

namespace Markdowser;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            GlobalState.Logger.LogFatal(e.ExceptionObject.ToString() ?? "Unknown error.");
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            GlobalState.Logger.LogFatal(e.Exception.ToString());
        };

        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
        {
            GlobalState.Logger.LogFatal(ex.ToString());
        });

        try
        {
            _ = BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            GlobalState.Logger.LogFatal(ex.ToString());
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        _ = IconProvider.Current
            .Register<FontAwesomeIconProvider>()
            .Register<MaterialDesignIconProvider>();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
    }
}