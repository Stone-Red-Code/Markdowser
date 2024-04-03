using Avalonia.Controls;
using Avalonia.Platform;

using Markdowser.Models;

using ReactiveUI;

using System.Reflection;

namespace Markdowser.ViewModels;

public class ViewModelBase : ReactiveObject
{
    public string Title => $"{nameof(Markdowser)} - {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";

    public WindowIcon Icon => Settings.Current.DarkMode ? new WindowIcon(AssetLoader.Open(new("avares://Markdowser/Assets/Markdowser-Dark-Transparent.ico"))) : new WindowIcon(AssetLoader.Open(new("avares://Markdowser/Assets/Markdowser-Light-Transparent.ico")));
}