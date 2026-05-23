using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components;
using ClosetApp.UI.Components.Clothing;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using ClosetApp.UI.States;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class ClothesTab : UserControl
{
    private readonly WardrobeViewModel _viewModel;

    public ClothesTab()
    {
        _viewModel = App.Services.GetRequiredService<WardrobeViewModel>();
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(WardrobeViewModel.FilteredClothes) or nameof(WardrobeViewModel.IsEmpty))
                _ = Dispatcher.BeginInvoke(UpdateCardWidth, System.Windows.Threading.DispatcherPriority.Loaded);
        };
        Loaded += (s, e) => _ = LoadClothesAsync();
        SizeChanged += (_, _) => UpdateCardWidth();
    }

    private async Task LoadClothesAsync()
    {
        await _viewModel.LoadClothesAsync();
        _ = Dispatcher.BeginInvoke(UpdateCardWidth, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void UpdateCardWidth()
    {
        if (ClothesList == null || ContentScroller == null) return;
        var masonry = FindVisualChild<MasonryPanel>(ClothesList);
        if (masonry == null) return;

        double availWidth = ContentScroller.ActualWidth - 128;
        if (availWidth <= 0) return;

        double gap = 20;
        int cols = (int)((availWidth + gap) / (260 + gap));
        cols = Math.Max(1, cols);

        double totalGap = gap * (cols - 1);
        double cardWidth = Math.Floor((availWidth - totalGap) / cols);
        cardWidth = Math.Clamp(cardWidth, 240, 300);

        masonry.ColumnWidth = cardWidth;
        masonry.Spacing = gap;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) return typed;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private void ToggleFilter_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleFilterExpanded();
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearFilters();
    }

    private void AddClothing_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new ClothingEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
                await _viewModel.AddClothingAsync(result.Entity!);
        });
    }

    private void BatchImport_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new BatchClothingImportPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved && result.Entity != null)
            {
                try
                {
                    var importResult = await _viewModel.ImportClothesAsync(result.Entity);
                    var summary = BatchClothingImportSummaryBuilder.Build(result.Entity, importResult.Clothes);
                    _ = Dispatcher.BeginInvoke(
                        () => ModalService.Instance.Show(new BatchClothingImportSummaryDialog(
                            summary,
                            () => _viewModel.SetQueueFilter(WardrobeQueueFilter.RecentlyImported))),
                        DispatcherPriority.Background);
                }
                catch (Exception ex)
                {
                    var feedback = WardrobeActionErrorPresenter.ForImport(ex);
                    ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
                }
            }
        });
    }

    private void BatchCompleteQueue_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanBatchCompleteCurrentQueue)
            return;

        EditorModal.Show(
            new BatchClothingCompletionPanel(_viewModel.FilteredClothes.ToList(), _viewModel.ActiveQueueLabel),
            async result =>
            {
                if (result.Type != EditorResultType.Saved || result.Entity == null)
                    return;

                try
                {
                    var summary = await _viewModel.CompleteCurrentQueueAsync(result.Entity);
                    ToastService.Instance.ShowSuccess($"已补全 {summary.UpdatedCount} 件衣服");
                }
                catch (Exception ex)
                {
                    var feedback = WardrobeActionErrorPresenter.ForBatchComplete(ex);
                    ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
                }
            });
    }

    private void BatchClearWardrobe_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.TotalCount == 0)
        {
            ToastService.Instance.ShowInfo("衣柜已经是空的了。");
            return;
        }

        EditorModal.Show(
            new BatchWardrobeClearPanel(_viewModel.AllClothes.ToList(), _viewModel.SelectedType),
            async result =>
            {
                if (result.Type != EditorResultType.Saved || result.Entity == null)
                    return;

                try
                {
                    var summary = await _viewModel.ClearWardrobeByTypesAsync(result.Entity);
                    ToastService.Instance.ShowSuccess(summary.DeletedCount == 0
                        ? "选中的分类里没有可清空的衣服。"
                        : $"已清空 {summary.DeletedCount} 件衣服");
                }
                catch (Exception ex)
                {
                    var feedback = WardrobeActionErrorPresenter.ForBatchClear(ex);
                    ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
                }
            });
    }

    private void ClothingCard_Edit(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not Clothing clothing) return;

        var oldImagePath = clothing.ImagePath;
        EditorModal.Show(new ClothingEditorPanel(clothing), async result =>
        {
            if (result.Type == EditorResultType.Saved)
            {
                await _viewModel.UpdateClothingAsync(result.Entity!, oldImagePath);
            }
            else if (result.Type == EditorResultType.Deleted)
            {
                await _viewModel.DeleteClothingAsync(clothing);
            }
        });
    }

    private async void ClothingCard_FavoriteToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not PremiumClothingCard card || card.DataContext is not Clothing clothing)
            return;

        try
        {
            await _viewModel.UpdateClothingAsync(clothing, clothing.ImagePath);
            ToastService.Instance.ShowSuccess(
                clothing.IsFavorite ? $"已收藏「{clothing.Name}」" : $"已取消收藏「{clothing.Name}」");
        }
        catch (Exception ex)
        {
            var reverted = !clothing.IsFavorite;
            clothing.IsFavorite = reverted;
            if (!clothing.IsFavorite && clothing.FavoriteLevel >= 4)
                clothing.FavoriteLevel = 3;
            else if (clothing.IsFavorite && clothing.FavoriteLevel < 4)
                clothing.FavoriteLevel = 4;

            card.RefreshFavoriteVisual();
            ToastService.Instance.ShowError("更新收藏失败", ex.Message);
        }
    }

    private async void ClothingCard_Delete(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not Clothing clothing) return;

        var confirmed = await ConfirmModal.ShowDeleteAsync(
            $"确定删除「{clothing.Name}」吗？",
            title: "删除衣服");
        if (!confirmed)
            return;

        try
        {
            await _viewModel.DeleteClothingAsync(clothing);
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForSingleDelete(ex, clothing.Name);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }
}
