using Markdowser.Models;
using Markdowser.Utilities;

using System;
using System.Windows.Input;

namespace Markdowser.Commands;

internal class HomeCommand : ICommand
{
#pragma warning disable CS0067 // The event 'HomeCommand.CanExecuteChanged' is never used

    public event EventHandler? CanExecuteChanged;

#pragma warning restore CS0067 // The event 'HomeCommand.CanExecuteChanged' is never used

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        GlobalState.Url = Settings.Current.HomeUrl ?? string.Empty;
    }
}