using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClosetApp.Application.DTOs;
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
            card.FavoriteToggled -= OutfitCard_FavoriteToggled;
            card.EditCompleted += OutfitCard_EditCompleted;
            card.DeleteRequested += OutfitCard_DeleteRequested;
            card.WornRequested += OutfitCard_WornRequested;
            card.FavoriteToggled += OutfitCard_FavoriteToggled;
        }
    }

    private void CreateOutfit_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new OutfitEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
            {
                try
                {
                    await LoadOutfitsAsync();
                    ToastService.Instance.ShowSuccess("已保存搭配", "新的搭配已经出现在列表里。");
                }
                catch (Exception ex)
                {
                    ToastService.Instance.ShowError("保存搭配后刷新失败", ex.Message);
                }
            }
        });
    }

    private async void OutfitCard_EditCompleted(object? sender, OutfitEntity outfit)
    {
        try
        {
            await _viewModel.RefreshAsync();
            ToastService.Instance.ShowSuccess($"已更新「{outfit.Name}」", "修改后的搭配已经同步到列表。");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("刷新搭配失败", ex.Message);
        }
    }

    private async void OutfitCard_DeleteRequested(object? sender, OutfitEntity outfit)
    {
        try
        {
            await _viewModel.DeleteOutfitAsync(outfit);
            ToastService.Instance.ShowSuccess($"已删除「{outfit.Name}」", "这套搭配已经从列表移除。");
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForOutfitDelete(ex, outfit.Name);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }

    private async void OutfitCard_WornRequested(object? sender, OutfitEntity outfit)
    {
        try
        {
            await _viewModel.RecordWornDateAsync(outfit, DateTime.Now);
            ToastService.Instance.ShowSuccess($"已记录穿过「{outfit.Name}」", "今天的穿着记录已经更新。");
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForOutfitRecord(ex, outfit.Name);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }

    private async void OutfitCard_FavoriteToggled(object? sender, RoutedEventArgs e)
    {
        if (sender is not OutfitCard card || card.Outfit == null) return;

        try
        {
            var isFav = await _viewModel.ToggleFavoriteAsync(card.Outfit);
            card.ApplyFavoriteVisual(card.Outfit);
            ToastService.Instance.ShowSuccess(isFav ? "已收藏" : "已取消收藏");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("操作失败", ex.Message);
        }
    }

    private async void RecommendedOutfitWorn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: RecommendedOutfitDto recommendation })
            return;

        try
        {
            await _viewModel.RecordWornDateAsync(recommendation.Outfit, DateTime.Now);
            ToastService.Instance.ShowSuccess($"已记录穿过「{recommendation.Name}」", "今日推荐已经同步到穿着记录。");
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForOutfitRecord(ex, recommendation.Name);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }

    private async void PrevMonth_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.MoveCalendarMonthAsync(-1);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("切换月份失败", ex.Message);
        }
    }

    private async void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.MoveCalendarMonthAsync(1);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("切换月份失败", ex.Message);
        }
    }

    private void ToggleHistory_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleHistoryExpanded();
    }

    private async void RefreshWeatherRecommendations_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.RefreshWeatherRecommendationsAsync();
            ToastService.Instance.ShowSuccess("已刷新天气推荐", "当前城市的天气和今日推荐已经更新。");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("刷新天气推荐失败", ex.Message);
        }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is not MainWindow window)
            return;

        window.NavigateToSettings();
    }

    private void CalendarDay_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: CalendarDayItem day })
            return;

        var dialog = new WornDayDetailsDialog(day.Date, day.Records);
        dialog.RecordsChanged += async (_, _) =>
        {
            try
            {
                await LoadOutfitsAsync();
            }
            catch (Exception ex)
            {
                ToastService.Instance.ShowError("刷新搭配失败", ex.Message);
            }
        };
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
