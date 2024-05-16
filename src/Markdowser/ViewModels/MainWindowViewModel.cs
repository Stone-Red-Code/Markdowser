﻿using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;

using Markdowser.Models;
using Markdowser.Processing;
using Markdowser.Processing.Processors;
using Markdowser.Utilities;
using Markdowser.ViewModels.Content;

using ReactiveUI;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Markdowser.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClient httpClient = new(new HttpClientHandler() { AllowAutoRedirect = true });
    private readonly ContentProcessorManager contentProcessorManager = new();
    private readonly CacheService cacheService = new CacheService();

    private ContentViewModelBase content;
    private TabItem currentTab = null!;
    private WindowState windowState;
    private bool showSidePanel;
    private bool isBusy;
    private int progress;
    public ObservableCollection<TabItem> Tabs => GlobalState.Tabs;

    public TabItem CurrentTab
    {
        get => currentTab;
        set
        {
            if (currentTab is not null)
            {
                currentTab.Tag = Url;
            }

            _ = this.RaiseAndSetIfChanged(ref currentTab!, value);

            Url = value?.Tag?.ToString() ?? string.Empty;

            FetchUrl(true);
        }
    }

    public WindowState WindowState
    {
        get => windowState;
        set => this.RaiseAndSetIfChanged(ref windowState, value);
    }

    public ContentViewModelBase Content
    {
        get => content;
        set => this.RaiseAndSetIfChanged(ref content, value);
    }

    public string Url
    {
        get => GlobalState.Url;
        set => GlobalState.SetUrl(this, value);
    }

    public bool ShowSidePanel
    {
        get => showSidePanel;
        set => this.RaiseAndSetIfChanged(ref showSidePanel, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        set => this.RaiseAndSetIfChanged(ref isBusy, value);
    }

    public int Progress
    {
        get => progress;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref progress, value);
            this.RaisePropertyChanged(nameof(ProgressIndeterminate));
        }
    }

    public SettingsViewModel SettingsViewModel => new SettingsViewModel();
    public RawHtmlViewModel RawHtmlViewModel => new RawHtmlViewModel(new());
    public RawMarkdownViewModel RawMarkdownViewModel => new RawMarkdownViewModel(() => new());
    public bool CloseTabEnabled => Tabs.Count > 1;
    public bool BackEnabled => GlobalState.BackHistory.Count > 0;
    public bool ForwardEnabled => GlobalState.ForwardHistory.Count > 0;
    public bool ProgressIndeterminate => Progress == 0;
    public ICommand Browse => ReactiveCommand.Create(() => FetchUrl(true));
    public ICommand Reload => ReactiveCommand.Create(() => FetchUrl(false));

    public ICommand Back => ReactiveCommand.Create(() =>
    {
        if (GlobalState.BackHistory.Count > 0)
        {
            GlobalState.ForwardHistory.Push(GlobalState.Url);
            Url = GlobalState.BackHistory.Pop();
            FetchUrl();

            this.RaisePropertyChanged(nameof(BackEnabled));
            this.RaisePropertyChanged(nameof(ForwardEnabled));
        }
    });

    public ICommand Forward => ReactiveCommand.Create(() =>
    {
        if (GlobalState.ForwardHistory.Count > 0)
        {
            GlobalState.BackHistory.Push(GlobalState.Url);
            Url = GlobalState.ForwardHistory.Pop();
            FetchUrl();

            this.RaisePropertyChanged(nameof(ForwardEnabled));
            this.RaisePropertyChanged(nameof(BackEnabled));
        }
    });

    public ICommand ToggleSidePanel => ReactiveCommand.Create(() => ShowSidePanel = !ShowSidePanel);

    public ICommand CloseTab => ReactiveCommand.Create(() =>
    {
        if (IsBusy)
        {
            WindowNotificationManager.Show(new Notification("Busy", "The browser is currently busy.", NotificationType.Warning));
            return;
        }

        if (Tabs.Count > 1)
        {
            int currentIndex = Tabs.IndexOf(CurrentTab);

            CurrentTab = currentIndex > 0 ? Tabs[currentIndex - 1] : Tabs[0];

            Tabs.RemoveAt(currentIndex);

            this.RaisePropertyChanged(nameof(CloseTabEnabled));
        }
    });

    public ICommand NewTab => ReactiveCommand.Create(() =>
    {
        if (IsBusy)
        {
            WindowNotificationManager.Show(new Notification("Busy", "The browser is currently busy.", NotificationType.Warning));
            return;
        }

        TabItem tab = new() { Header = "New Tab", Name = Guid.NewGuid().ToString() };
        Tabs.Add(tab);
        CurrentTab = tab;
        this.RaisePropertyChanged(nameof(CloseTabEnabled));
    });

    public ICommand ToggleFullScreen => ReactiveCommand.Create(() => WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen);

    private MarkdownContentViewModel DefaultContent => new("New Tab", new StringBuilder()
        .AppendLine($"![Logo](avares://Markdowser/Assets/Markdowser-{(Settings.Current.DarkMode ? "Dark" : "Light")}-Transparent.png)")
        .AppendLine()
        .AppendLine($"{nameof(Markdowser)} {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}\n")
        .AppendLine("A markdown web browser.\n")
        .AppendLine("[GitHub](https://github.me.stone-red.net/Markdowser)").ToString());

    public MainWindowViewModel()
    {
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        content = DefaultContent;

        contentProcessorManager.RegisterProcessor(new HtmlProcessor());
        contentProcessorManager.RegisterProcessor(new CommonImageProcessor());

        GlobalState.UrlChanged += (sender, url) =>
        {
            this.RaisePropertyChanged(nameof(ForwardEnabled));
            this.RaisePropertyChanged(nameof(BackEnabled));
            this.RaisePropertyChanged(nameof(Url));

            if (sender == this)
            {
                return;
            }

            FetchUrl();
        };

        GlobalState.ContentReload += (sender, _) =>
        {
            // Update icon when dark mode changes
            this.RaisePropertyChanged(nameof(Icon));
            this.RaisePropertyChanged(nameof(Content));
        };
    }

    private static bool IsValidHttpUri(string uriString, [NotNullWhen(true)] out Uri? uri)
    {
        return Uri.TryCreate(uriString, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private void FetchUrl(bool useCache = true)
    {
        if (IsBusy)
        {
            WindowNotificationManager.Show(new Notification("Busy", "The browser is currently busy.", NotificationType.Warning));
            return;
        }

        if (string.IsNullOrWhiteSpace(Url))
        {
            Content = DefaultContent;
            Logger.LogDebug("URL is empty.");
            CurrentTab.Header = "New Tab";
            return;
        }

        if (!IsValidHttpUri(Url, out _))
        {
            if (Url.StartsWith("//"))
            {
                Url = $"https:{Url}";
            }
            else
            {
                // Search with duckduckgo
                try
                {
                    Url = string.Format(Settings.Current.SearchEngineUrl, Uri.EscapeDataString(Url));
                }
                catch (FormatException ex)
                {
                    WindowNotificationManager.Show(new Notification("Invalid Search Engine URL", $"{ex.Message}", NotificationType.Error));
                }
            }
        }

        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        if (!httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Settings.Current.UserAgent))
        {
            WindowNotificationManager.Show(new Notification("Invalid User Agent", "Failed to set user agent.", NotificationType.Error));
            return;
        }

        IsBusy = true;
        Progress = 0;

        _ = Task.Run(async () =>
        {
            Logger.LogInfo($"Fetching URL: {Url}");

            try
            {
                ContentViewModelBase? cachedContent = cacheService.Get(Url);
                if (cachedContent is not null && useCache)
                {
                    Content = cachedContent;
                }
                else
                {
                    HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(Url);

                    string? newUrl = httpResponseMessage.RequestMessage?.RequestUri?.ToString();

                    if (newUrl is not null && newUrl != Url)
                    {
                        string oldUrl = Url;
                        Dispatcher.UIThread.Post(() => WindowNotificationManager.Show(new Notification("Redirected", $"Redirected from\n{oldUrl}\nto\n{newUrl}", NotificationType.Warning)));
                        Url = newUrl;
                    }

                    if (!httpResponseMessage.IsSuccessStatusCode)
                    {
                        IsBusy = false;

                        StringBuilder errorMessage = new();

                        _ = errorMessage.AppendLine($"# {(int)httpResponseMessage.StatusCode} {httpResponseMessage.StatusCode}");
                        _ = errorMessage.AppendLine($"Failed to fetch URL: {httpResponseMessage.ReasonPhrase}");

                        Content = new MarkdownContentViewModel("Error", errorMessage.ToString());
                        return;
                    }

                    Content = await contentProcessorManager.ProcessContent(httpResponseMessage, new Progress<ProcessingProgress>(p => Progress = p.Percentage));
                    cacheService.Set(Url, Content);
                }

                Dispatcher.UIThread.Post(() => CurrentTab.Header = Content.Title);

                Logger.LogInfo($"Fetched URL: {Url}");
            }
            catch (HttpRequestException e)
            {
                StringBuilder errorMessage = new();

                if (e.StatusCode is not null)
                {
                    _ = errorMessage.AppendLine($"# {(int)e.StatusCode} {e.StatusCode}");
                    Dispatcher.UIThread.Post(() => CurrentTab.Header = $"Error: {(int)e.StatusCode} {e.StatusCode}");
                }
                else
                {
                    Dispatcher.UIThread.Post(() => CurrentTab.Header = $"Error: {e.GetType().Name}");
                }

                _ = errorMessage.AppendLine($"# {e.HttpRequestError}");
                _ = errorMessage.AppendLine($"Failed to fetch URL: {e.Message}");

                Content = new MarkdownContentViewModel("Error", errorMessage.ToString());

                Logger.LogError(e.Message);
            }
            catch (Exception e)
            {
                StringBuilder errorMessage = new();

                _ = errorMessage.AppendLine($"# {e.GetType().Name}");
                _ = errorMessage.AppendLine($"Failed to fetch URL: {e.Message}");

                Content = new MarkdownContentViewModel("Error", errorMessage.ToString());
                Dispatcher.UIThread.Post(() => CurrentTab.Header = $"Error: {e.GetType().Name}");

                Logger.LogError(e.Message);
            }
            finally
            {
                IsBusy = false;
            }
        });
    }
}