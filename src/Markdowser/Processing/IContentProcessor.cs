using Markdowser.ViewModels;

using System;
using System.Net;
using System.Threading.Tasks;

namespace Markdowser.Processing;

internal interface IContentProcessor
{
    bool CanProcess(HttpWebResponse response);

    Task<ContentViewModelBase> Process(HttpWebResponse response, IProgress<ProcessingProgress> progress);
}