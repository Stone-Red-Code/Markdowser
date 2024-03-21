using Markdowser.Utilities;

using System;
using System.Windows.Input;

namespace Markdowser.Commands;

internal class HomeCommand : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        GlobalState.Url = Settings.Current.HomeUrl ?? string.Empty;
    }
}