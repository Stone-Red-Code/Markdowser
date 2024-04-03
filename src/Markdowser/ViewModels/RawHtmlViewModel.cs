using System.Text;

namespace Markdowser.ViewModels;

public class RawHtmlViewModel(StringBuilder html) : ViewModelBase
{
    public StringBuilder Html => html;
}