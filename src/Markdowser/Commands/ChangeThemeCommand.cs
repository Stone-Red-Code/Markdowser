using Avalonia;
using Avalonia.Styling;

using Markdowser.Models;
using Markdowser.Utilities;

using System;
using System.Windows.Input;

namespace Markdowser.Commands;

internal class ChangeThemeCommand : ICommand
{
#pragma warning disable CS0067 // The event 'ChangeThemeCommand.CanExecuteChanged' is never used

    public event EventHandler? CanExecuteChanged;

#pragma warning restore CS0067 // The event 'ChangeThemeCommand.CanExecuteChanged' is never used

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
            GlobalState.InvokeThemeChanged();
        }
    }
}