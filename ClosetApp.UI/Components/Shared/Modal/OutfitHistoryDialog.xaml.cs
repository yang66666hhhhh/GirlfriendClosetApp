using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClosetApp.UI.Services;
using ClosetApp.UI.States;
using ClosetApp.UI.ViewModels;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class OutfitHistoryDialog : UserControl
{
    private readonly OutfitsViewModel _viewModel;
    private bool _isRecentSectionCollapsed;

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

    private async void RecentPreviewButton_Click(object sender, RoutedEventArgs e)
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

    private void ToggleRecentSection_Click(object sender, RoutedEventArgs e)
    {
        _isRecentSectionCollapsed = !_isRecentSectionCollapsed;
        UpdateRecentSectionState();
    }

    private void UpdateRecentSectionState()
    {
        if (RecentSectionContent == null || ToggleRecentSectionButton == null || RecentSectionSummaryText == null)
            return;

        RecentSectionContent.Visibility = _isRecentSectionCollapsed ? Visibility.Collapsed : Visibility.Visible;
        ToggleRecentSectionButton.Content = _isRecentSectionCollapsed ? "展开" : "收起";

        var recordCount = _viewModel.RecentWornRecords.Count;
        var currentItem = CurrentPreviewPanel?.DataContext as RecentWornListItem ?? _viewModel.SelectedRecentWornRecord;

        RecentSectionSummaryText.Text = recordCount switch
        {
            0 => "还没有最近穿着记录。",
            _ when currentItem == null => $"最近 {recordCount} 条穿着记录。",
            _ => $"最近 {recordCount} 条穿着记录 · 当前 {currentItem.OutfitName}"
        };
    }
}
