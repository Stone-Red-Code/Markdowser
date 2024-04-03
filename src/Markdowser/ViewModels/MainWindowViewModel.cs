using Avalonia.Controls;
using Avalonia.Threading;

using HtmlAgilityPack;

using Markdowser.Models;
using Markdowser.Utilities;

using ReactiveUI;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Markdowser.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClient httpClient = new(new HttpClientHandler() { AllowAutoRedirect = true });
    private readonly ReverseMarkdown.Converter markdownConverter = new();

    private readonly StringBuilder html = new();
    private StringBuilder? content;
    private TabItem currentTab = null!;
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

            Url = currentTab?.Tag?.ToString() ?? string.Empty;

            FetchUrl();
        }
    }

    public StringBuilder Content
    {
        get => content ?? DefaultContent;
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
    public RawHtmlViewModel RawHtmlViewModel => new RawHtmlViewModel(html);
    public RawMarkdownViewModel RawMarkdownViewModel => new RawMarkdownViewModel(() => Content);
    public bool CloseTabEnabled => Tabs.Count > 1;
    public bool BackEnabled => GlobalState.BackHistory.Count > 0;
    public bool ForwardEnabled => GlobalState.ForwardHistory.Count > 0;
    public bool ProgressIndeterminate => Progress == 0;
    public ICommand Browse => ReactiveCommand.Create(FetchUrl);

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
        if (Tabs.Count > 1)
        {
            int currentIndex = Tabs.IndexOf(CurrentTab);
            _ = Tabs.Remove(CurrentTab);

            CurrentTab = currentIndex > 0 ? Tabs[currentIndex - 1] : Tabs[0];
            this.RaisePropertyChanged(nameof(CloseTabEnabled));
        }
    });

    public ICommand NewTab => ReactiveCommand.Create(() =>
    {
        TabItem tab = new() { Header = "New Tab", Name = Guid.NewGuid().ToString() };
        Tabs.Add(tab);
        CurrentTab = tab;
        this.RaisePropertyChanged(nameof(CloseTabEnabled));
    });

    private StringBuilder DefaultContent => new StringBuilder()
        .AppendLine($"![Logo](avares://Markdowser/Assets/Markdowser-{(Settings.Current.DarkMode ? "Dark" : "Light")}-Transparent.png)")
        .AppendLine()
        .AppendLine($"{nameof(Markdowser)} {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}\n")
        .AppendLine("A markdown web browser.\n")
        .AppendLine("[GitHub](https://github.me.stone-red.net/Markdowser)");

    public MainWindowViewModel()
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"{nameof(Markdowser)}/{Assembly.GetExecutingAssembly().GetName().Version?.ToString()}");
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        markdownConverter.Config.UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.Bypass;
        markdownConverter.Config.SuppressDivNewlines = false;
        markdownConverter.Config.SmartHrefHandling = false;

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
            // Updatre icon when dark mode changes
            this.RaisePropertyChanged(nameof(Icon));
            this.RaisePropertyChanged(nameof(Content));
        };
    }

    private static bool IsValidHttpUri(string uriString, [NotNullWhen(true)] out Uri? uri)
    {
        return Uri.TryCreate(uriString, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    [GeneratedRegex("!\\[(.*?)\\]\\((.*?)\\)")]
    private static partial Regex MarkdownImage();

    private void FetchUrl()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            content = null;
            _ = html.Clear();
            ContentChanged();
            Debug.WriteLine("URL is empty.");
            CurrentTab.Header = "New Tab";
            return;
        }

        if (!IsValidHttpUri(Url, out _))
        {
            // Search with duckduckgo
            Url = $"https://duckduckgo.com/html/?kd=-1&k1=-1&q={Uri.EscapeDataString(Url)}";
        }

        content ??= new StringBuilder();
        _ = content.Clear();
        _ = html.Clear();

        IsBusy = true;
        Progress = 0;

        _ = Task.Run(async () =>
        {
            Debug.WriteLine("Fetching URL...");

            try
            {
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(Url);

                Url = httpResponseMessage.RequestMessage?.RequestUri?.ToString() ?? Url;

                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    IsBusy = false;
                    _ = content.AppendLine($"# {(int)httpResponseMessage.StatusCode} {httpResponseMessage.StatusCode}");
                    _ = content.AppendLine($"Failed to fetch URL: {httpResponseMessage.ReasonPhrase}");
                    ContentChanged();
                    return;
                }

                long length = httpResponseMessage.Content.Headers.ContentLength ?? 0;

                using Stream contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
                using StreamReader streamReader = new(contentStream);

                while (!streamReader.EndOfStream)
                {
                    _ = html.AppendLine(await streamReader.ReadLineAsync());
                    Progress = (int)((double)contentStream.Position / length * 100);
                }

                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html.ToString());
                string title = htmlDoc.DocumentNode.SelectSingleNode("html/head/title")?.InnerText ?? Url;

                Dispatcher.UIThread.Post(() =>
                {
                    CurrentTab.Header = title?.Trim() ?? Url;
                });
            }
            catch (HttpRequestException e)
            {
                IsBusy = false;

                if (e.StatusCode is not null)
                {
                    _ = content.AppendLine($"# {(int)e.StatusCode} {e.StatusCode}");
                }

                _ = content.AppendLine($"# {e.HttpRequestError}");
                _ = content.AppendLine($"Failed to fetch URL: {e.Message}");
                ContentChanged();
            }
            catch (Exception e)
            {
                IsBusy = false;
                _ = content.AppendLine($"# {e.GetType().Name}");
                _ = content.AppendLine($"Failed to fetch URL: {e.Message}");
                ContentChanged();
            }

            Progress = 0;

            Debug.WriteLine("Converting HTML to markdown...");

            string markdown = markdownConverter.Convert(html.ToString());

            Debug.WriteLine("Processing markdown...");

            int currentLine = 0;

            string[] lines = markdown.Split('\n');
            foreach (string line in lines)
            {
                Progress = (int)((double)currentLine / lines.Length * 100);
                currentLine++;

                string trimmedLine = line.Trim();

                if (trimmedLine.All(c => c == '#'))
                {
                    _ = Content.Append(trimmedLine);
                    continue;
                }
                else if (MarkdownImage().IsMatch(trimmedLine))
                {
                    _ = content.AppendLine()
                    .Append(trimmedLine)
                    .AppendLine()
                    .AppendLine();
                    continue;
                }

                _ = content.AppendLine(trimmedLine);
            }

            ContentChanged();

            IsBusy = false;
        });
    }

    private void ContentChanged()
    {
        this.RaisePropertyChanged(nameof(Content));
        this.RaisePropertyChanged(nameof(RawHtmlViewModel));
        this.RaisePropertyChanged(nameof(RawMarkdownViewModel));
    }
}