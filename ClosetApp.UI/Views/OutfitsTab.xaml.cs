using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Outfit.Controls;
using ClosetApp.UI.Components.Outfit.Editor;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using ClosetApp.UI.States;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using OutfitEntity = ClosetApp.Domain.Entities.Outfit;

namespace ClosetApp.UI.Views;

public partial class OutfitsTab : UserControl
{
    private readonly OutfitsViewModel _viewModel;

    public OutfitsTab()
    {
        _viewModel = App.Services.GetRequiredService<OutfitsViewModel>();
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(OutfitsViewModel.Outfits) or nameof(OutfitsViewModel.IsEmpty))
            {
                _ = Dispatcher.BeginInvoke(AttachCardHandlers, System.Windows.Threading.DispatcherPriority.Loaded);
            }
        };
        Loaded += async (s, e) => await LoadOutfitsAsync();
    }

    private async Task LoadOutfitsAsync()
    {
        await _viewModel.LoadOutfitsAsync();
        _ = Dispatcher.BeginInvoke(AttachCardHandlers, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    public Task RefreshAsync() => _viewModel.RefreshAsync();

    private void AttachCardHandlers()
    {
        foreach (var item in OutfitsList.Items)
        {
            var container = OutfitsList.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
            if (container == null)
                continue;

            var card = FindVisualChild<OutfitCard>(container);
            if (card == null)
                continue;

            card.EditCompleted -= OutfitCard_EditCompleted;
            card.DeleteRequested -= OutfitCard_DeleteRequested;
            card.WornRequested -= OutfitCard_WornRequested;
            card.EditCompleted += OutfitCard_EditCompleted;
            card.DeleteRequested += OutfitCard_DeleteRequested;
            card.WornRequested += OutfitCard_WornRequested;
        }
    }

    private void CreateOutfit_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new OutfitEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
                await LoadOutfitsAsync();
        });
    }

    private async void OutfitCard_EditCompleted(object? sender, OutfitEntity outfit)
    {
        await _viewModel.RefreshAsync();
    }

    private async void OutfitCard_DeleteRequested(object? sender, OutfitEntity outfit)
    {
        await _viewModel.DeleteOutfitAsync(outfit);
    }

    private async void OutfitCard_WornRequested(object? sender, OutfitEntity outfit)
    {
        await _viewModel.RecordWornDateAsync(outfit, DateTime.Now);
    }

    private async void PrevMonth_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.MoveCalendarMonthAsync(-1);
    }

    private async void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.MoveCalendarMonthAsync(1);
    }

    private void ToggleHistory_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleHistoryExpanded();
    }

    private void CalendarDay_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: CalendarDayItem day })
            return;

        var dialog = new WornDayDetailsDialog(day.Date, day.Records);
        dialog.RecordsChanged += async (_, _) => await LoadOutfitsAsync();
        ModalService.Instance.Show(dialog);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }
}
