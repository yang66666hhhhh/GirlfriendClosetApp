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

        var dialog = new WornDayDetailsDialog(day.Date, day.Records);
        dialog.RecordsChanged += async (_, _) =>
        {
            await _viewModel.RefreshAsync();
            SyncCurrentPreview(_viewModel.SelectedRecentWornRecord);
        };
        ModalService.Instance.Show(dialog);
    }

    private void SyncCurrentPreview(RecentWornListItem? item)
    {
        CurrentPreviewPanel.DataContext = item;
        CurrentPreviewPanel.Visibility = item == null ? Visibility.Collapsed : Visibility.Visible;
    }
}
