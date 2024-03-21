using Markdowser.Utilities;

using ReactiveUI;

using System;
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

    private StringBuilder? content;

    private bool isBusy;

    private int progress;
    public string Title => $"{nameof(Markdowser)} - {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";

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
            this.RaisePropertyChanged(nameof(Content));
            Debug.WriteLine("URL is empty.");
            return;
        }

        if (!IsValidHttpUri(Url, out _))
        {
            // Search with duckduckgo
            Url = $"https://duckduckgo.com/html/?kd=-1&k1=-1&q={Uri.EscapeDataString(Url)}";
        }

        content ??= new StringBuilder();
        _ = content.Clear();

        IsBusy = true;
        Progress = 0;

        _ = Task.Run(async () =>
        {
            Debug.WriteLine("Fetching URL...");

            StringBuilder html = new();

            try
            {
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(Url);

                Url = httpResponseMessage.RequestMessage?.RequestUri?.ToString() ?? Url;

                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    IsBusy = false;
                    _ = content.AppendLine($"# {(int)httpResponseMessage.StatusCode} {httpResponseMessage.StatusCode}");
                    _ = content.AppendLine($"Failed to fetch URL: {httpResponseMessage.ReasonPhrase}");
                    this.RaisePropertyChanged(nameof(Content));
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
                this.RaisePropertyChanged(nameof(Content));
            }
            catch (Exception e)
            {
                IsBusy = false;
                _ = content.AppendLine($"# {e.GetType().Name}");
                _ = content.AppendLine($"Failed to fetch URL: {e.Message}");
                this.RaisePropertyChanged(nameof(Content));
            }

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

                //this.RaisePropertyChanged(nameof(Content));
            }

            Debug.WriteLine("Done processing.");
            this.RaisePropertyChanged(nameof(Content));

            IsBusy = false;
        });
    }
}