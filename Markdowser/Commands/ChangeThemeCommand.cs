using Avalonia;
using Avalonia.Styling;

using Markdowser.Utilities;

using System;
using System.Windows.Input;

namespace Markdowser.Commands;

internal class ChangeThemeCommand : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        Application? app = Application.Current;
        if (app is not null)
        {
            ThemeVariant theme = app.ActualThemeVariant;
            app.RequestedThemeVariant = theme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
            Settings.Current.DarkMode = app.RequestedThemeVariant == ThemeVariant.Dark;
            GlobalState.ReloadContent(this);
        }
    }
}