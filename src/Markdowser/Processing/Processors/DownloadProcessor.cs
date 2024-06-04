using CuteUtils;

using Markdowser.Utilities;
using Markdowser.ViewModels;
using Markdowser.ViewModels.Content;

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Markdowser.Processing.Processors;

internal class DownloadProcessor : IContentProcessor
{
    public string Name => "Download Processor";

    public string Description => "Processes downloadable content";

    public bool CanProcess(HttpContentHeaders httpContentHeaders)
    {
        bool canProcess = httpContentHeaders.ContentDisposition?.FileName is not null;

        // Check for common media types that are downloadable
        if (!canProcess)
        {
            canProcess = httpContentHeaders.ContentType?.MediaType switch
            {
                "application/octet-stream" => true,
                "application/force-download" => true,
                "application/zip" => true,
                "application/x-zip-compressed" => true,
                "application/x-zip" => true,
                "application/x-compressed" => true,
                "multipart/x-zip" => true,
                _ => false
            };
        }

        return canProcess;
    }

    public async Task<ContentViewModelBase> Process(HttpResponseMessage httpResponseMessage, IProgress<ProcessingProgress> progress)
    {
        string? fileName = httpResponseMessage.Content.Headers.ContentDisposition?.FileName;
        long contentLength = httpResponseMessage.Content.Headers.ContentLength ?? 0;

        fileName ??= httpResponseMessage.RequestMessage?.RequestUri?.Segments[^1].ToFileName();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "download.bin";
        }

        string tempFileName = fileName + ".tmp";

        GlobalState.Logger.LogDebug($"Downloading {fileName}...");

        using Stream contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
        using FileStream fileStream = new(Path.Combine(Configuration.DownloadPath, tempFileName), FileMode.Create, FileAccess.Write);

        while (true)
        {
            byte[] buffer = new byte[81920];
            int bytesRead = await contentStream.ReadAsync(buffer);

            if (bytesRead == 0)
            {
                break;
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

            progress.Report(new ProcessingProgress(fileStream.Position, contentLength, message: $"Downloading \"{fileName}\"", true));
        }

        fileStream.Close();

        File.Move(Path.Combine(Configuration.DownloadPath, tempFileName), Path.Combine(Configuration.DownloadPath, fileName), overwrite: true);

        string content = $"# Downloaded File\n\nDownloaded file: [{fileName}]({Path.Combine(Configuration.DownloadPath, fileName)}) to {Configuration.DownloadPath}";
        return new MarkdownContentViewModel(fileName, content)
        {
            Cacheable = false
        };
    }
}