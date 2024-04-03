using Markdowser.ViewModels;

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Markdowser.Processing;

internal class ContentProcessorManager
{
    private readonly List<IContentProcessor> processors = [];

    public void RegisterProcessor(IContentProcessor processor)
    {
        processors.Add(processor);
    }

    public Task<ContentViewModelBase> ProcessContent(HttpWebResponse response, IProgress<ProcessingProgress> progress)
    {
        IContentProcessor? processor = processors.Find(processor => processor.CanProcess(response)) ?? throw new InvalidOperationException("No processor found for the given response.");
        return processor.Process(response, progress);
    }
}