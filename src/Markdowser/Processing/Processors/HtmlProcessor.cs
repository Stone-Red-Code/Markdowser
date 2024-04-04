using HtmlAgilityPack;

using Markdowser.ViewModels;
using Markdowser.ViewModels.Content;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Markdowser.Processing.Processors;

internal partial class HtmlProcessor : IContentProcessor
{
    private readonly ReverseMarkdown.Converter markdownConverter = new();

    public HtmlProcessor()
    {
        markdownConverter.Config.UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.Bypass;
        markdownConverter.Config.SuppressDivNewlines = false;
        markdownConverter.Config.SmartHrefHandling = false;
    }

    public bool CanProcess(HttpContentHeaders httpContentHeaders)
    {
        return httpContentHeaders.ContentType?.MediaType == "text/html";
    }

    public async Task<ContentViewModelBase> Process(HttpResponseMessage httpResponseMessage, IProgress<ProcessingProgress> progress)
    {
        StringBuilder html = new();
        StringBuilder finalMarkdown = new();

        long length = httpResponseMessage.Content.Headers.ContentLength ?? 0;

        using Stream contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
        using StreamReader streamReader = new(contentStream);

        while (!streamReader.EndOfStream)
        {
            _ = html.AppendLine(await streamReader.ReadLineAsync());

            progress.Report(new ProcessingProgress(length, contentStream.Position));
        }

        HtmlDocument htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html.ToString());
        string title = htmlDoc.DocumentNode.SelectSingleNode("html/head/title")?.InnerText?.Trim() ?? httpResponseMessage.RequestMessage?.RequestUri?.ToString() ?? "Untitled";

        Debug.WriteLine("Converting HTML to markdown...");

        string markdown = markdownConverter.Convert(html.ToString());

        Debug.WriteLine("Processing markdown...");

        int currentLine = 0;

        string[] lines = markdown.Split('\n');

        foreach (string line in lines)
        {
            progress.Report(new ProcessingProgress(lines.Length, currentLine));
            currentLine++;

            string trimmedLine = line.Trim();

            if (trimmedLine.All(c => c == '#'))
            {
                _ = finalMarkdown.Append(trimmedLine);
                continue;
            }
            else if (MarkdownImage().IsMatch(trimmedLine))
            {
                _ = finalMarkdown.AppendLine()
                .Append(trimmedLine)
                .AppendLine()
                .AppendLine();
                continue;
            }

            _ = finalMarkdown.AppendLine(trimmedLine);
        }

        return new MarkdownContentViewModel(title, finalMarkdown.ToString());
    }

    [GeneratedRegex("!\\[(.*?)\\]\\((.*?)\\)")]
    private static partial Regex MarkdownImage();
}