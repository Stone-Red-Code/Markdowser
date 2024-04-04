using Markdowser.ViewModels;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Markdowser.Processing;

internal class ContentProcessorManager
{
    private readonly List<IContentProcessor> processors = [];

    public void RegisterProcessor(IContentProcessor processor)
    {
        processors.Add(processor);
    }

    public Task<ContentViewModelBase> ProcessContent(HttpResponseMessage httpResponseMessage, IProgress<ProcessingProgress> progress)
    {
        IContentProcessor? processor = processors.Find(processor => processor.CanProcess(httpResponseMessage.Content.Headers)) ?? throw new InvalidOperationException($"No processor found for content type {httpResponseMessage.Content.Headers.ContentType}");
        return processor.Process(httpResponseMessage, progress);
    }
}