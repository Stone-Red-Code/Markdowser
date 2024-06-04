using Markdowser.ViewModels;
using Markdowser.ViewModels.Content;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Markdowser.Processing.Processors;

internal class CommonImageProcessor : IContentProcessor
{
    public string Name => "Common Image Processor";

    public string Description => "Processes common image types (e.g. PNG, JPEG, GIF)";

    public bool CanProcess(HttpContentHeaders httpContentHeaders)
    {
        if (httpContentHeaders.ContentType?.MediaType == "image/svg+xml")
        {
            return false;
        }

        return httpContentHeaders.ContentType?.MediaType?.StartsWith("image/") == true;
    }

    public async Task<ContentViewModelBase> Process(HttpResponseMessage httpResponseMessage, IProgress<ProcessingProgress> progress)
    {
        return new CommonImageContentViewModel(httpResponseMessage.RequestMessage?.RequestUri?.Host?.ToString() ?? "Image", await httpResponseMessage.Content.ReadAsStreamAsync());
    }
}