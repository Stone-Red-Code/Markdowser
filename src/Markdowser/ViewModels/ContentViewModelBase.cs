using CuteUtils.Logging;

using Markdowser.Utilities;

namespace Markdowser.ViewModels;

public abstract class ContentViewModelBase(string title)
{
    public string Title { get; } = title;

    public string RawContent { get; init; } = string.Empty;

    public Logger Logger => GlobalState.Logger;
}