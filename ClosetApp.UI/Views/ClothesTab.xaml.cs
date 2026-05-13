using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Components;
using ClosetApp.UI.Components.Clothing;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using ClosetApp.UI.States;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ClosetApp.UI.Views;

public partial class ClothesTab : UserControl
{
    private readonly IClothingService _clothingService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ClothesTabState _state = new();
    private bool _isFilterExpanded;

    public ClothesTab()
    {
        InitializeComponent();
        _clothingService = App.Services.GetRequiredService<IClothingService>();
        _imageStorageService = App.Services.GetRequiredService<IImageStorageService>();
        Loaded += (s, e) => _ = LoadClothesAsync();
    }

    private async Task LoadClothesAsync()
    {
        _state.BeginLoad();
        var clothes = await _clothingService.GetAllClothesAsync();
        _state.SetClothes(clothes);
        Log.Debug("Loaded clothes. Total={TotalCount}, Filtered={FilteredCount}", _state.AllClothes.Count, _state.FilteredCount);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (!AreViewControlsReady()) return;

        if (_state.IsEmpty)
        {
            EmptyState.Visibility = Visibility.Visible;
            WardrobeSummary.Visibility = Visibility.Collapsed;
            ClothesList.Visibility = Visibility.Collapsed;
            TxtCount.Text = "";
            TxtTotalCount.Text = "0 件";
            TxtFilteredCount.Text = "0 件";
        }
        else
        {
            EmptyState.Visibility = Visibility.Collapsed;
            WardrobeSummary.Visibility = Visibility.Visible;
            ClothesList.Visibility = Visibility.Visible;

            TxtTotalCount.Text = $"{_state.AllClothes.Count} 件";
            TxtFilteredCount.Text = $"{_state.FilteredCount} 件";
            TxtCount.Text = $"{_state.FilterSummary} · {_state.FilteredCount} 件结果";
            TxtFilterHint.Text = _state.HasActiveFilters
                ? "当前已应用筛选；点「清除」可以回到完整衣柜。"
                : "选择分类后，衣服列表会立即收窄。";

            ClothesList.ItemsSource = _state.FilteredClothes;

            Dispatcher.BeginInvoke(() => UpdateCardWidth(), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private bool AreViewControlsReady()
    {
        return EmptyState != null
            && WardrobeSummary != null
            && ClothesList != null
            && TxtCount != null
            && TxtTotalCount != null
            && TxtFilteredCount != null
            && TxtFilterHint != null;
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

    private void Category_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb) return;
        var selectedCategories = rb.Name switch
        {
            "ChipAll" => null,
            "ChipTop" => new[] { DisplayCategory.Topwear },
            "ChipBottom" => new[] { DisplayCategory.Bottom },
            "ChipDress" => new[] { DisplayCategory.Dress },
            "ChipShoes" => new[] { DisplayCategory.Footwear },
            "ChipAccessory" => new[] { DisplayCategory.Accessory },
            _ => null
        };
        _state.SetSelectedCategories(selectedCategories);
        UpdateUI();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        _state.SetSearchText(textBox.Text);
        UpdateUI();
    }

    private void ToggleFilter_Click(object sender, RoutedEventArgs e)
    {
        _isFilterExpanded = !_isFilterExpanded;
        if (FilterPanel == null || ToggleFilterButton == null) return;
        FilterPanel.Visibility = _isFilterExpanded ? Visibility.Visible : Visibility.Collapsed;
        ToggleFilterButton.Content = _isFilterExpanded ? "收起筛选" : "展开筛选";
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        if (InlineSearch != null)
            InlineSearch.Text = "";
        if (ChipAll != null)
            ChipAll.IsChecked = true;
        _state.SetSearchText("");
        _state.SetSelectedCategories(null);
        UpdateUI();
    }

    private void AddClothing_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new ClothingEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
                await _clothingService.AddClothingAsync(result.Entity!);
            await LoadClothesAsync();
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
                await _clothingService.UpdateClothingAsync(result.Entity!);
                await DeleteReplacedImageAsync(oldImagePath, result.Entity!.ImagePath);
            }
            else if (result.Type == EditorResultType.Deleted)
            {
                await DeleteClothingWithImageAsync(clothing);
            }
            await LoadClothesAsync();
        });
    }

    private async void ClothingCard_Delete(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not Clothing clothing) return;

        var confirmed = await ShowDeleteConfirmAsync($"确定删除「{clothing.Name}」吗？");
        if (!confirmed)
            return;

        await DeleteClothingWithImageAsync(clothing);
        await LoadClothesAsync();
    }

    private async Task DeleteClothingWithImageAsync(Clothing clothing)
    {
        var imagePath = clothing.ImagePath;
        Log.Information("Deleting clothing {ClothingId} ({ClothingName})", clothing.Id, clothing.Name);
        await _clothingService.DeleteClothingAsync(clothing.Id);
        await DeleteStoredImageAsync(imagePath);
    }

    private async Task DeleteReplacedImageAsync(string? oldImagePath, string? newImagePath)
    {
        if (string.Equals(oldImagePath, newImagePath, StringComparison.OrdinalIgnoreCase))
            return;

        await DeleteStoredImageAsync(oldImagePath);
    }

    private async Task DeleteStoredImageAsync(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        try
        {
            await _imageStorageService.DeleteImageWithThumbnailAsync(imagePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to delete stored clothing image {ImagePath}", imagePath);
            // Deleting the database record is the source of truth; stale image cleanup should not block the UI.
        }
    }

    private static async Task<bool> ShowDeleteConfirmAsync(string detail)
    {
        var dialog = new ConfirmDialog
        {
            Title = "删除衣服",
            Body = "删除后将无法恢复。",
            Detail = detail,
            ConfirmText = "删除",
            CancelText = "取消"
        };

        var tcs = new TaskCompletionSource<bool>();
        void ConfirmedHandler(object? sender, EventArgs e) => tcs.TrySetResult(true);
        void CancelledHandler(object? sender, EventArgs e) => tcs.TrySetResult(false);
        dialog.Confirmed += ConfirmedHandler;
        dialog.Cancelled += CancelledHandler;
        ModalService.Instance.Show(dialog);
        var result = await tcs.Task;
        ModalService.Instance.Hide();
        return result;
    }
}
