using Markdowser.Utilities;

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace Markdowser.Commands;

public class HyperlinkCommand : ICommand
{
#pragma warning disable CS0067 // The event 'HyperlinkCommand.CanExecuteChanged' is never used

    public event EventHandler? CanExecuteChanged;

#pragma warning restore CS0067 // The event 'HyperlinkCommand.CanExecuteChanged' is never used

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        if (parameter is string url)
        {
            GlobalState.Logger.LogDebug($"Hyperlink clicked: {url}");

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                GlobalState.CurrentTabState.Url = url;
            }
            else if (Uri.IsWellFormedUriString(url, UriKind.Relative) || url.StartsWith('/'))
            {
                GlobalState.CurrentTabState.Url = new Uri(new Uri(GlobalState.CurrentTabState.Url), url).ToString();
            }
            else if (File.Exists(url))
            {
                ProcessStartInfo processStartInfo = new()
                {
                    FileName = url,
                    UseShellExecute = true
                };
                try
                {
                    _ = Process.Start(processStartInfo);
                }
                catch (Exception ex)
                {
                    GlobalState.Logger.LogError(ex.Message);
                }
            }
            else
            {
                GlobalState.Logger.LogDebug($"Invalid URL: {url}");
            }
        }
    }
}