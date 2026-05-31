using System.Windows;
using System.Windows.Controls;

namespace ClosetApp.UI.Components.Clothing;

public partial class WardrobeSummaryPanel : UserControl
{
    public WardrobeSummaryPanel()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? ToggleFilterRequested;
    public event RoutedEventHandler? ClearFilterRequested;
    public event RoutedEventHandler? BatchCompleteQueueRequested;
    public event RoutedEventHandler? ClearCurrentCategoryRequested;

    private void ToggleFilter_Click(object sender, RoutedEventArgs e)
    {
        ToggleFilterRequested?.Invoke(this, e);
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        ClearFilterRequested?.Invoke(this, e);
    }

    private void BatchCompleteQueue_Click(object sender, RoutedEventArgs e)
    {
        BatchCompleteQueueRequested?.Invoke(this, e);
    }

    private void ClearCurrentCategory_Click(object sender, RoutedEventArgs e)
    {
        ClearCurrentCategoryRequested?.Invoke(this, e);
    }
}
