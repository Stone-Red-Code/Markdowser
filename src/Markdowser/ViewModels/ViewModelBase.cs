using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Platform;

using CuteUtils.Logging;

using Markdowser.Models;
using Markdowser.Utilities;

using ReactiveUI;

using System.Reflection;

namespace Markdowser.ViewModels;

public class ViewModelBase : ReactiveObject
{
    private WindowNotificationManager? windowNotificationManager;
    public string Title => $"{nameof(Markdowser)} - {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";

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