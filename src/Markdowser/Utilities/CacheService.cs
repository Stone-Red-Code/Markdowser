using Markdowser.ViewModels;
using System.Collections.Generic;

namespace Markdowser.Utilities
{
    namespace Markdowser.Utilities
    {
        public class CacheService
        {
            private Dictionary<string, ContentViewModelBase> cache = new Dictionary<string, ContentViewModelBase>();

            public ContentViewModelBase Get(string url)
            {
                if (cache.ContainsKey(url))
                {
                    return cache[url];
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
    }
}