using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
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

    public event EventHandler<int>? ClothingCountChanged;

    public ClothesTab()
    {
        _viewModel = App.Services.GetRequiredService<WardrobeViewModel>();
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(WardrobeViewModel.DisplayedClothes) or nameof(WardrobeViewModel.IsEmpty))
                _ = Dispatcher.BeginInvoke(UpdateCardWidth, System.Windows.Threading.DispatcherPriority.Loaded);

            if (e.PropertyName == nameof(WardrobeViewModel.TotalCount))
                ClothingCountChanged?.Invoke(this, _viewModel.TotalCount);
        };
        Loaded += async (_, _) => await RefreshAsync();
        SizeChanged += (_, _) => UpdateCardWidth();
    }

    public async Task RefreshAsync()
    {
        try
        {
            await _viewModel.LoadClothesAsync();
            ClothingCountChanged?.Invoke(this, _viewModel.TotalCount);
            _ = Dispatcher.BeginInvoke(UpdateCardWidth, System.Windows.Threading.DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("刷新衣柜失败", ex.Message);
        }
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
        EditorModal.Show(new ClothingEditorPanel(), HandleAddClothingResultAsync);
    }

    private void BatchImport_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new BatchClothingImportPanel(), HandleBatchImportResultAsync);
    }

    private void BatchCompleteQueue_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanBatchCompleteCurrentQueue)
            return;

        EditorModal.Show(
            new BatchClothingCompletionPanel(_viewModel.FilteredClothes.ToList(), _viewModel.ActiveQueueLabel),
            HandleBatchCompleteResultAsync);
    }

    private void OpenBatchClearWardrobe_Click(object sender, RoutedEventArgs e)
    {
        OpenClearWardrobePanel(initialType: null);
    }

    private void ClearCurrentCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedType == null)
            return;

        OpenClearWardrobePanel(_viewModel.SelectedType);
    }

    private void LoadMoreClothes_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.LoadMoreClothes();
    }

    private void OpenClearWardrobePanel(ClothingType? initialType)
    {
        if (_viewModel.TotalCount == 0)
        {
            ToastService.Instance.ShowInfo("衣柜已经是空的了。");
            return;
        }

        EditorModal.Show(
            new BatchWardrobeClearPanel(_viewModel.AllClothes.ToList(), initialType),
            HandleClearWardrobeResultAsync);
    }

    private void ClothingCard_Edit(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not Clothing clothing) return;

        var oldImagePath = clothing.ImagePath;
        EditorModal.Show(new ClothingEditorPanel(clothing), result => HandleEditClothingResultAsync(clothing, oldImagePath, result));
    }

    private async void ClothingCard_FavoriteToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not PremiumClothingCard card || card.DataContext is not Clothing clothing)
            return;

        try
        {
            var message = await _viewModel.UpdateFavoriteAsync(clothing);
            ToastService.Instance.ShowSuccess(message);
        }
        catch (Exception ex)
        {
            clothing.FavoriteLevel = card.LastFavoriteLevelBeforeToggle;

            card.RefreshFavoriteVisual();
            ToastService.Instance.ShowError("更新收藏失败", ex.Message);
        }
    }

    private async void ClothingCard_Delete(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not Clothing clothing) return;

        await ConfirmAndDeleteClothingAsync(clothing);
    }

    private async Task HandleAddClothingResultAsync(EditorResult<Clothing> result)
    {
        if (result.Type != EditorResultType.Saved || result.Entity == null)
            return;

        await _viewModel.AddClothingAsync(result.Entity);
        ToastService.Instance.ShowSuccess($"已添加「{result.Entity.Name}」");
    }

    private async Task HandleEditClothingResultAsync(
        Clothing originalClothing,
        string? oldImagePath,
        EditorResult<Clothing> result)
    {
        if (result.Type == EditorResultType.Saved && result.Entity != null)
        {
            await _viewModel.UpdateClothingAsync(result.Entity, oldImagePath);
            ToastService.Instance.ShowSuccess($"已更新「{result.Entity.Name}」");
            return;
        }

        if (result.Type == EditorResultType.Deleted)
            await DeleteClothingWithFeedbackAsync(originalClothing);
    }

    private async Task HandleBatchImportResultAsync(EditorResult<BatchClothingImportRequest> result)
    {
        if (result.Type != EditorResultType.Saved || result.Entity == null)
            return;

        var summary = await _viewModel.ImportClothesAndBuildSummaryAsync(result.Entity);
        ToastService.Instance.ShowSuccess($"已导入 {summary.ImportedCount} 件衣物", "可以在下方查看导入结果。");
        _ = Dispatcher.BeginInvoke(
            () => ModalService.Instance.Show(new BatchClothingImportSummaryDialog(
                summary,
                () => _viewModel.SetQueueFilter(WardrobeQueueFilter.RecentlyImported))),
            DispatcherPriority.Background);
    }

    private async Task HandleBatchCompleteResultAsync(EditorResult<BatchClothingCompletionRequest> result)
    {
        if (result.Type != EditorResultType.Saved || result.Entity == null)
            return;

        var message = await _viewModel.CompleteCurrentQueueAndBuildSuccessMessageAsync(result.Entity);
        ToastService.Instance.ShowSuccess(message);
    }

    private async Task HandleClearWardrobeResultAsync(EditorResult<BatchWardrobeClearRequest> result)
    {
        if (result.Type != EditorResultType.Saved || result.Entity == null)
            return;

        var message = await _viewModel.ClearWardrobeByTypesAndBuildSuccessMessageAsync(result.Entity);
        ToastService.Instance.ShowSuccess(message);
    }

    private async Task ConfirmAndDeleteClothingAsync(Clothing clothing)
    {
        var confirmed = await ConfirmModal.ShowDeleteAsync(
            $"确定删除「{clothing.Name}」吗？",
            title: "删除衣服");
        if (!confirmed)
            return;

        await DeleteClothingWithFeedbackAsync(clothing);
    }

    private async Task DeleteClothingWithFeedbackAsync(Clothing clothing)
    {
        try
        {
            await _viewModel.DeleteClothingAsync(clothing);
            ToastService.Instance.ShowSuccess($"已删除「{clothing.Name}」", "这件衣服已经从衣柜里移除。");
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForSingleDelete(ex, clothing.Name);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }
}
