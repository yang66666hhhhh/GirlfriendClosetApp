using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ClosetApp.UI.Services;

public static class ComboBoxWheelGuard
{
    public static void HandlePreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (FindComboBox(e.OriginalSource as DependencyObject) is not ComboBox comboBox)
        {
            return;
        }

        if (comboBox.IsDropDownOpen)
        {
            return;
        }

        e.Handled = true;

        var reroutedEvent = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = e.Source
        };

        if (FindAncestor<ScrollViewer>(comboBox) is { } scrollViewer)
        {
            scrollViewer.RaiseEvent(reroutedEvent);
            return;
        }

        if (FindAncestor<UIElement>(comboBox) is { } ancestor)
        {
            ancestor.RaiseEvent(reroutedEvent);
        }
    }

    private static ComboBox? FindComboBox(DependencyObject? start)
    {
        return FindAncestor<ComboBox>(start);
    }

    private static T? FindAncestor<T>(DependencyObject? start) where T : DependencyObject
    {
        var current = start;
        while (current != null)
        {
            if (current is T match)
            {
                return match;
            }

            current = current switch
            {
                Visual visual => VisualTreeHelper.GetParent(visual),
                Visual3D visual3D => VisualTreeHelper.GetParent(visual3D),
                FrameworkContentElement contentElement => contentElement.Parent,
                _ => null
            };
        }

        return null;
    }
}
