using System.Windows;
using System.Windows.Media;

namespace ClosetApp.UI.Components.Shared;

public static class VisualTreeHelperExtensions
{
    public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;

            var found = FindVisualChild<T>(child);
            if (found != null)
                return found;
        }

        return null;
    }

    public static T? FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T fe && fe.Name == name)
                return fe;

            var found = FindChildByName<T>(child, name);
            if (found != null)
                return found;
        }

        return null;
    }

    public static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T result)
                return result;

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }
}
