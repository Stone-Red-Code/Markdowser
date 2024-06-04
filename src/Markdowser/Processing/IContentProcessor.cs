using Markdowser.ViewModels;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Markdowser.Processing;

internal interface IContentProcessor
{
    string Name { get; }

    string Description { get; }

    bool CanProcess(HttpContentHeaders httpContentHeaders);

    Task<ContentViewModelBase> Process(HttpResponseMessage httpResponseMessage, IProgress<ProcessingProgress> progress);
}