using Markdowser.ViewModels;

using System;
using System.Collections.Generic;

namespace Markdowser.Models;

public class TabState
{
    public event EventHandler? UrlChanged;

    private string url = string.Empty;

    public Stack<string> BackHistory { get; } = new();

    public Stack<string> ForwardHistory { get; } = new();

    public string Url
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
            UrlChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ContentViewModelBase? Content { get; set; }

    public void SetUrl(string url, object? sender = null)
    {
        this.url = url;
        UrlChanged?.Invoke(sender ?? this, EventArgs.Empty);
    }
}