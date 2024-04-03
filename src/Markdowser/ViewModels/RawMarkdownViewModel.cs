using System;
using System.Text;

namespace Markdowser.ViewModels;

public class RawMarkdownViewModel(Func<StringBuilder> markdown) : ViewModelBase
{
    public StringBuilder Markdown => markdown();
}