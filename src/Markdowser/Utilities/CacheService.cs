using Markdowser.ViewModels;

using System.Collections.Generic;

namespace Markdowser.Utilities;

public class CacheService
{
    private readonly Dictionary<string, ContentViewModelBase> cache = [];

    public ContentViewModelBase? Get(string url)
    {
        if (cache.TryGetValue(url, out ContentViewModelBase? value))
        {
            return value;
        }
        else
        {
            return null;
        }
    }

    public void Set(string url, ContentViewModelBase content)
    {
        cache[url] = content;
    }
}