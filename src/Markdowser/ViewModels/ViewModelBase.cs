using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform;

using CuteUtils.Logging;

using Markdowser.Models;
using Markdowser.Utilities;

using ReactiveUI;

using System.Diagnostics;
using System.Reflection;

namespace Markdowser.ViewModels;

public class ViewModelBase : ReactiveObject
{
    private WindowNotificationManager? windowNotificationManager;

    public string Title
    {
        get
        {
            string title = nameof(Markdowser);
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string? version = fileVersionInfo.ProductVersion;

#if DEBUG
            return $"{title} - Dev {version}";
#else
            return $"{title} - {version}";
#endif
        }
    }

    public WindowIcon Icon => Settings.Current.DarkMode ? new WindowIcon(AssetLoader.Open(new("avares://Markdowser/Assets/Markdowser-Dark-Transparent.ico"))) : new WindowIcon(AssetLoader.Open(new("avares://Markdowser/Assets/Markdowser-Light-Transparent.ico")));

    public WindowNotificationManager WindowNotificationManager => windowNotificationManager!;

    public Logger Logger => GlobalState.Logger;

    protected internal void InitializeWindowNotificationManager(Window window)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(window);
        windowNotificationManager = new WindowNotificationManager(topLevel)
        {
            Position = NotificationPosition.TopRight
        };
    }
}