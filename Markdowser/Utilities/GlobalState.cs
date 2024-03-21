using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Markdowser.Utilities;

[SuppressMessage("Major Code Smell", "S4220:Events should have proper arguments", Justification = "<Pending>")]
internal static class GlobalState
{
    public static event EventHandler<string>? UrlChanged;

    public static event EventHandler? ContentReload;

    private static string url = string.Empty;

    public static Stack<string> BackHistory { get; } = new();

    public static Stack<string> ForwardHistory { get; } = new();

    public static string Url
    {
        get => url;
        set
        {
            if (url != value)
            {
                BackHistory.Push(url);
                ForwardHistory.Clear();
            }

            url = value;
            UrlChanged?.Invoke(null, url);
        }
    }

    internal static void SetUrl(object sender, string url)
    {
        GlobalState.url = url;
        UrlChanged?.Invoke(sender, url);
    }

    internal static void ReloadContent(object sender)
    {
        ContentReload?.Invoke(sender, EventArgs.Empty);
    }
}