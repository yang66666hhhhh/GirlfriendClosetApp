using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components;
using ClosetApp.UI.Components.Clothing;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
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
        _viewModel.PropertyChanged += (_, _) => Dispatcher.Invoke(UpdateUI);
        Loaded += (s, e) => _ = LoadClothesAsync();
    }

    private async Task LoadClothesAsync()
    {
        await _viewModel.LoadClothesAsync();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (!AreViewControlsReady()) return;

        if (_viewModel.IsEmpty)
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

            TxtTotalCount.Text = $"{_viewModel.TotalCount} 件";
            TxtFilteredCount.Text = $"{_viewModel.FilteredCount} 件";
            TxtCount.Text = $"{_viewModel.FilterSummary} · {_viewModel.FilteredCount} 件结果";
            TxtFilterHint.Text = _viewModel.FilterHint;

            ClothesList.ItemsSource = _viewModel.FilteredClothes;
            SeasonAll.IsChecked = _viewModel.SelectedSeason == null;
            SeasonSpring.IsChecked = _viewModel.SelectedSeason == Season.Spring;
            SeasonSummer.IsChecked = _viewModel.SelectedSeason == Season.Summer;
            SeasonAutumn.IsChecked = _viewModel.SelectedSeason == Season.Autumn;
            SeasonWinter.IsChecked = _viewModel.SelectedSeason == Season.Winter;
            FavoriteOnlyCheckBox.IsChecked = _viewModel.FavoriteOnly;

            RenderTagFilters();

            Dispatcher.BeginInvoke(() => UpdateCardWidth(), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        ApplyFilterPanelState();
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
        _viewModel.SetSelectedCategories(selectedCategories);
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        _viewModel.SearchText = textBox.Text;
    }

    private void Season_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || rb.IsChecked != true)
            return;

        Season? season = rb.Name switch
        {
            "SeasonAll" => null,
            "SeasonSpring" => Season.Spring,
            "SeasonSummer" => Season.Summer,
            "SeasonAutumn" => Season.Autumn,
            "SeasonWinter" => Season.Winter,
            _ => null
        };

        _viewModel.SetSelectedSeason(season);
    }

    private void FavoriteOnly_Changed(object sender, RoutedEventArgs e)
    {
        _viewModel.SetFavoriteOnly(FavoriteOnlyCheckBox.IsChecked == true);
    }

    private void ToggleFilter_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleFilterExpanded();
        ApplyFilterPanelState();
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        if (InlineSearch != null)
            InlineSearch.Text = "";
        if (ChipAll != null)
            ChipAll.IsChecked = true;
        if (SeasonAll != null)
            SeasonAll.IsChecked = true;
        if (FavoriteOnlyCheckBox != null)
            FavoriteOnlyCheckBox.IsChecked = false;
        _viewModel.ClearFilters();
        RenderTagFilters();
        ApplyFilterPanelState();
    }

    private void AddClothing_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new ClothingEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
                await _viewModel.AddClothingAsync(result.Entity!);
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

    private async void ClothingCard_Delete(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not Clothing clothing) return;

        var confirmed = await ShowDeleteConfirmAsync($"确定删除「{clothing.Name}」吗？");
        if (!confirmed)
            return;

        await _viewModel.DeleteClothingAsync(clothing);
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

    private void ApplyFilterPanelState()
    {
        if (FilterPanel == null || ToggleFilterButton == null)
            return;

        FilterPanel.Visibility = _viewModel.IsFilterExpanded ? Visibility.Visible : Visibility.Collapsed;
        ToggleFilterButton.Content = _viewModel.IsFilterExpanded ? "收起筛选" : "展开筛选";
    }

    private void RenderTagFilters()
    {
        if (TagFilterPanel == null)
            return;

        TagFilterPanel.Children.Clear();

        foreach (var tag in _viewModel.AvailableTags)
        {
            var checkBox = new CheckBox
            {
                Content = tag.Name,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(14, 8, 14, 8),
                IsChecked = _viewModel.SelectedTagIds.Contains(tag.Id)
            };

            checkBox.Checked += (_, _) => _viewModel.ToggleTag(tag.Id, true);
            checkBox.Unchecked += (_, _) => _viewModel.ToggleTag(tag.Id, false);
            TagFilterPanel.Children.Add(checkBox);
        }
    }
}
