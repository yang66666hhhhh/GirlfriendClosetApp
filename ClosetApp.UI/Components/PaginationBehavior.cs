using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClosetApp.UI.Components;

public static class PaginationBehavior
{
    public static readonly DependencyProperty PageSizeProperty =
        DependencyProperty.RegisterAttached("PageSize", typeof(int), typeof(PaginationBehavior),
            new PropertyMetadata(20));

    public static readonly DependencyProperty LoadMoreCommandProperty =
        DependencyProperty.RegisterAttached("LoadMoreCommand", typeof(ICommand), typeof(PaginationBehavior),
            new PropertyMetadata(null, OnLoadMoreCommandChanged));

    public static readonly DependencyProperty HasMoreItemsProperty =
        DependencyProperty.RegisterAttached("HasMoreItems", typeof(bool), typeof(PaginationBehavior),
            new PropertyMetadata(true));

    public static int GetPageSize(DependencyObject obj) => (int)obj.GetValue(PageSizeProperty);
    public static void SetPageSize(DependencyObject obj, int value) => obj.SetValue(PageSizeProperty, value);

    public static ICommand GetLoadMoreCommand(DependencyObject obj) => (ICommand)obj.GetValue(LoadMoreCommandProperty);
    public static void SetLoadMoreCommand(DependencyObject obj, ICommand value) => obj.SetValue(LoadMoreCommandProperty, value);

    public static bool GetHasMoreItems(DependencyObject obj) => (bool)obj.GetValue(HasMoreItemsProperty);
    public static void SetHasMoreItems(DependencyObject obj, bool value) => obj.SetValue(HasMoreItemsProperty, value);

    private static void OnLoadMoreCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer)
        {
            if (e.OldValue != null)
                scrollViewer.ScrollChanged -= OnScrollChanged;

            if (e.NewValue != null)
                scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
            return;

        var command = GetLoadMoreCommand(scrollViewer);
        var hasMore = GetHasMoreItems(scrollViewer);

        if (command == null || !hasMore || !command.CanExecute(null))
            return;

        // 当滚动到底部附近时加载更多
        var threshold = scrollViewer.ViewportHeight * 2;
        if (scrollViewer.VerticalOffset + scrollViewer.ViewportHeight >= scrollViewer.ExtentHeight - threshold)
        {
            command.Execute(null);
        }
    }
}
