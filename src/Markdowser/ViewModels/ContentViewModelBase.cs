namespace Markdowser.ViewModels;

public abstract class ContentViewModelBase(string title)
{
    public string Title { get; } = title;
}