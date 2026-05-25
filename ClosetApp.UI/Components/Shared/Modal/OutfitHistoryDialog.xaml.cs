using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClosetApp.UI.Services;
using ClosetApp.UI.States;
using ClosetApp.UI.ViewModels;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class OutfitHistoryDialog : UserControl
{
    private readonly OutfitsViewModel _viewModel;

    public OutfitHistoryDialog(OutfitsViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OutfitHistoryDialog_Loaded;
    }

    private void OutfitHistoryDialog_Loaded(object sender, RoutedEventArgs e)
    {
        SyncCurrentPreview(_viewModel.SelectedRecentWornRecord);
        UpdateRecentSectionState();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ModalService.Instance.Hide();
    }

    private async void RecentWornItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: RecentWornListItem item })
            return;

        await _viewModel.FocusHistoryRecordAsync(item.RecordId, item.WornDate);
        SyncCurrentPreview(item);
    }

    private async void PrevMonth_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.MoveCalendarMonthAsync(-1);
    }

    private async void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.MoveCalendarMonthAsync(1);
    }

    private async void CalendarDay_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: CalendarDayItem day })
            return;

        await _viewModel.FocusHistoryDateAsync(day.Date);
        SyncCurrentPreview(_viewModel.SelectedRecentWornRecord);

        var dialog = new WornDayDetailsDialog(day.Date, day.Records, isEmbedded: true);
        dialog.RecordsChanged += async (_, _) =>
        {
            await _viewModel.RefreshAsync();
            SyncCurrentPreview(_viewModel.SelectedRecentWornRecord);
        };
        dialog.CloseRequested += (_, _) => CloseDayDetailsOverlay();
        OpenDayDetailsOverlay(dialog);
    }

    private void SyncCurrentPreview(RecentWornListItem? item)
    {
        CurrentPreviewPanel.DataContext = item;
        CurrentPreviewPanel.Visibility = item == null ? Visibility.Collapsed : Visibility.Visible;
        UpdateRecentSectionState();
    }

    private void OpenDayDetailsOverlay(WornDayDetailsDialog dialog)
    {
        DayDetailsHost.Content = dialog;
        DayDetailsOverlay.Visibility = Visibility.Visible;
    }

    private void CloseDayDetailsOverlay()
    {
        DayDetailsHost.Content = null;
        DayDetailsOverlay.Visibility = Visibility.Collapsed;
    }

    private void DayDetailsBackdrop_Click(object sender, MouseButtonEventArgs e)
    {
        CloseDayDetailsOverlay();
        e.Handled = true;
    }

    private void OpenRecentSection_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        if (RecentWornPopup.IsOpen)
        {
            RecentWornPopup.IsOpen = false;
            return;
        }

        OpenRecentWornPopup(element);
    }

    private void UpdateRecentSectionState()
    {
        if (OpenRecentSectionButton == null || RecentSectionSummaryText == null)
            return;

        var recordCount = _viewModel.RecentWornRecords.Count;
        var latestItem = _viewModel.RecentWornRecords.FirstOrDefault();

        RecentSectionSummaryText.Text = recordCount switch
        {
            0 => "还没有最近穿着记录。",
            _ when latestItem == null => $"最近 {recordCount} 条记录",
            _ => $"最近 {recordCount} 条记录 · 最新 {latestItem.DateText}"
        };
    }

    private void OpenRecentWornPopup(FrameworkElement placementTarget)
    {
        if (CurrentPreviewPanel.DataContext == null)
            SyncCurrentPreview(_viewModel.SelectedRecentWornRecord);

        RecentWornPopup.PlacementTarget = placementTarget;
        // 让浮层优先向左展开，避免把主日历区域压得太满。
        RecentWornPopup.HorizontalOffset = placementTarget.ActualWidth - 468;
        RecentWornPopup.VerticalOffset = 8;
        RecentWornPopup.IsOpen = true;
    }

    private void RecentWornPopup_Opened(object sender, System.EventArgs e)
    {
        UpdateRecentSectionTriggerVisual(isOpen: true);
    }

    private void RecentWornPopup_Closed(object sender, System.EventArgs e)
    {
        UpdateRecentSectionTriggerVisual(isOpen: false);
    }

    private void UpdateRecentSectionTriggerVisual(bool isOpen)
    {
        if (OpenRecentSectionButton == null)
            return;

        OpenRecentSectionButton.Background = ResolveBrush(
            isOpen ? "PrimaryLightBrush" : null,
            isOpen ? "#F4F8FF" : "#FBFDFF");
        OpenRecentSectionButton.BorderBrush = ResolveBrush(
            isOpen ? "PrimaryBrush" : null,
            isOpen ? "#5B83D8" : "#E1EAF5");
        OpenRecentSectionButton.Foreground = ResolveBrush(
            isOpen ? "PrimaryBrush" : "TextSecondaryBrush",
            isOpen ? "#5B83D8" : "#6E7685");
    }

    private Brush ResolveBrush(string? resourceKey, string fallbackHex)
    {
        if (!string.IsNullOrWhiteSpace(resourceKey) && TryFindResource(resourceKey) is Brush brush)
            return brush;

        return (Brush)new BrushConverter().ConvertFromString(fallbackHex)!;
    }
}
