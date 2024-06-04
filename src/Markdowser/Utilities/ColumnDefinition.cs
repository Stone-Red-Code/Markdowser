using Avalonia;

namespace Markdowser.Utilities;

internal static class ColumnDefinition
{
    public static readonly AttachedProperty<bool> IsVisibleProperty = AvaloniaProperty.RegisterAttached<Avalonia.Controls.ColumnDefinition, bool>("IsVisible", typeof(ColumnDefinition), true, coerce: (element, visibility) =>
    {
        Avalonia.Controls.GridLength? lastWidth = element.GetValue(LastWidthProperty!);
        if (visibility && lastWidth is { })
        {
            _ = element.SetValue(Avalonia.Controls.ColumnDefinition.WidthProperty, lastWidth);
        }
        else if (!visibility)
        {
            _ = element.SetValue(LastWidthProperty!, element.GetValue(Avalonia.Controls.ColumnDefinition.WidthProperty));
            _ = element.SetValue(Avalonia.Controls.ColumnDefinition.WidthProperty, ZeroWidth);
        }
        return visibility;
    });

    private static readonly Avalonia.Controls.GridLength ZeroWidth = new Avalonia.Controls.GridLength(0, Avalonia.Controls.GridUnitType.Pixel);

    private static readonly AttachedProperty<Avalonia.Controls.GridLength?> LastWidthProperty = AvaloniaProperty.RegisterAttached<Avalonia.Controls.ColumnDefinition, Avalonia.Controls.GridLength?>("LastWidth", typeof(ColumnDefinition), default);

    public static bool GetIsVisible(Avalonia.Controls.ColumnDefinition columnDefinition)
    {
        return columnDefinition.GetValue(IsVisibleProperty);
    }

    public static void SetIsVisible(Avalonia.Controls.ColumnDefinition columnDefinition, bool visibility)
    {
        _ = columnDefinition.SetValue(IsVisibleProperty, visibility);
    }
}