using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Platform;
using Avalonia.Threading;

using Markdown.Avalonia.Utils;

using Markdowser.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Markdowser.Utilities;

public class HttpPathResolver : IPathResolver
{
    private readonly HttpClient httpClient = new();
    public string? AssetPathRoot { get; set; }
    public IEnumerable<string>? CallerAssemblyNames { get; set; }

    public async Task<Stream?>? ResolveImageResource(string relativeOrAbsolutePath)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
        {
            return GetLogo();
        }

        if (relativeOrAbsolutePath.StartsWith("avares://"))
        {
            return AssetLoader.Open(new Uri(relativeOrAbsolutePath))!;
        }

        if (!Uri.IsWellFormedUriString(relativeOrAbsolutePath, UriKind.Absolute))
        {
            relativeOrAbsolutePath = new Uri(new Uri(GlobalState.Url), relativeOrAbsolutePath).ToString();
        }

        GlobalState.Logger.LogDebug($"Resolving image: {relativeOrAbsolutePath}");

        HttpResponseMessage httpResponseMessage;

        try
        {
            httpResponseMessage = await httpClient.GetAsync(relativeOrAbsolutePath)!;
        }
        catch (Exception ex)
        {
            // error with inluding url and message
            ShowError($"Failed to fetch image: {ex.Message}\n{relativeOrAbsolutePath} - ");
            return GetLogo();
        }

        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            ShowError($"Failed to fetch image: {httpResponseMessage.ReasonPhrase}\n{relativeOrAbsolutePath}");
            return GetLogo();
        }

        return await httpResponseMessage.Content.ReadAsStreamAsync();
    }

    private static Stream GetLogo()
    {
        if (Settings.Current.DarkMode)
        {
            return AssetLoader.Open(new Uri("avares://Markdowser/Assets/Markdowser-Dark-Transparent.png"))!;
        }

        return AssetLoader.Open(new Uri("avares://Markdowser/Assets/Markdowser-Light-Transparent.png"))!;
    }

    private static void ShowError(string message)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.DataContext is ViewModels.MainWindowViewModel vm)
        {
            Dispatcher.UIThread.Post(() => vm.WindowNotificationManager.Show(new Notification("Error", message, NotificationType.Error)));
        }

        GlobalState.Logger.LogError(message);
    }
}