using Avalonia.Controls;
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

    private TabItem currentTab = null!;
    private WindowState windowState;
    private bool showSidePanel;
    private bool isBusy;
    private int progress;
    public ObservableCollection<TabItem> Tabs { get; } = [new TabItem() { Header = "New Tab" }];

    public TabItem CurrentTab
    {
        get => currentTab;
        set
        {
            CurrentTabState.UrlChanged -= UrlChanged;

            _ = this.RaiseAndSetIfChanged(ref currentTab!, value);
            this.RaisePropertyChanged(nameof(CurrentTabState));
            this.RaisePropertyChanged(nameof(Url));
            this.RaisePropertyChanged(nameof(BackEnabled));
            this.RaisePropertyChanged(nameof(ForwardEnabled));

            GlobalState.CurrentTabState = CurrentTabState;
            CurrentTabState.UrlChanged += UrlChanged;

            FetchUrl(true);
        }
    }

    public TabState CurrentTabState
    {
        get
        {
            if (CurrentTab is null)
            {
                return new();
            }

            return Dispatcher.UIThread.Invoke(() =>
            {
                CurrentTab.Tag ??= new TabState();
                return (TabState)CurrentTab.Tag;
            });
        }
    }

    public WindowState WindowState
    {
        get => windowState;
        set => this.RaiseAndSetIfChanged(ref windowState, value);
    }

    public ContentViewModelBase Content
    {
        get => CurrentTabState.Content!;
        set
        {
            CurrentTabState.Content = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(RawContent));
        }
    }

    public string RawContent => string.IsNullOrWhiteSpace(Content.RawContent) ? "No raw content." : Content.RawContent;

    public string Url
    {
        get => CurrentTabState.Url;
        set => CurrentTabState.SetUrl(value, this);
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
    public bool CloseTabEnabled => Tabs.Count > 1;
    public bool BackEnabled => CurrentTabState.BackHistory.Count > 0;
    public bool ForwardEnabled => CurrentTabState.ForwardHistory.Count > 0;
    public bool ProgressIndeterminate => Progress == 0;
    public ICommand Browse => ReactiveCommand.Create(() => FetchUrl(true));
    public ICommand Reload => ReactiveCommand.Create(() => FetchUrl(false));

    public ICommand Back => ReactiveCommand.Create(() =>
    {
        if (CurrentTabState.BackHistory.Count > 0)
        {
            CurrentTabState.ForwardHistory.Push(CurrentTabState.Url);
            Url = CurrentTabState.BackHistory.Pop();
            FetchUrl();

            this.RaisePropertyChanged(nameof(BackEnabled));
            this.RaisePropertyChanged(nameof(ForwardEnabled));
        }
    });

    public ICommand Forward => ReactiveCommand.Create(() =>
    {
        if (CurrentTabState.ForwardHistory.Count > 0)
        {
            CurrentTabState.BackHistory.Push(CurrentTabState.Url);
            Url = CurrentTabState.ForwardHistory.Pop();
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

        Tabs[0].Tag = new TabState() { Content = DefaultContent };

        contentProcessorManager.RegisterProcessor(new HtmlProcessor());
        contentProcessorManager.RegisterProcessor(new CommonImageProcessor());

        GlobalState.ThemeChanged += (s, e) =>
        {
            this.RaisePropertyChanged(nameof(Icon));
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
                    string oldUrl = Url;

                    if (newUrl is not null && newUrl != Url)
                    {
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

                    if (oldUrl != Url)
                    {
                        cacheService.Set(oldUrl, Content);
                    }

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

    private void UrlChanged(object? sender, EventArgs e)
    {
        this.RaisePropertyChanged(nameof(ForwardEnabled));
        this.RaisePropertyChanged(nameof(BackEnabled));
        this.RaisePropertyChanged(nameof(Url));

        if (sender == this)
        {
            return;
        }

        FetchUrl();
    }
}