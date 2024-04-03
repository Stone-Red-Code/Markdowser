namespace Markdowser.ViewModels.Content;

public class MarkdownContentViewModel(string title, string markdown) : ContentViewModelBase(title)
{
    public string Markdown { get; } = markdown;
}