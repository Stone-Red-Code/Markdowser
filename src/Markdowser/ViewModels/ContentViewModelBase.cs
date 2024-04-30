using CuteUtils.Logging;

using Markdowser.Utilities;

namespace Markdowser.ViewModels;

public abstract class ContentViewModelBase(string title)
{
    public string Title { get; } = title;

    public Logger Logger { get; } = GlobalState.Logger;
}