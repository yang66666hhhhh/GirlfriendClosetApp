using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components;
using ClosetApp.UI.Components.Clothing;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.States;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class ClothesTab : UserControl
{
    private readonly IClothingService _clothingService;
    private readonly ClothesTabState _state = new();

    public ClothesTab()
    {
        InitializeComponent();
        _clothingService = App.Services.GetRequiredService<IClothingService>();
        Loaded += (s, e) => _ = LoadClothesAsync();
    }

    private async Task LoadClothesAsync()
    {
        _state.BeginLoad();
        var clothes = await _clothingService.GetAllClothesAsync();
        _state.SetClothes(clothes);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (TxtCount == null) return;

        if (_state.IsEmpty)
        {
            EmptyState.Visibility = Visibility.Visible;
            HeroBanner.Visibility = Visibility.Collapsed;
            ClothesList.Visibility = Visibility.Collapsed;
            TxtCount.Text = "";
        }
        else
        {
            EmptyState.Visibility = Visibility.Collapsed;
            HeroBanner.Visibility = Visibility.Visible;
            ClothesList.Visibility = Visibility.Visible;

            TxtCount.Text = $"{_state.FilteredCount} 件";

            ClothesList.ItemsSource = _state.FilteredClothes;

            Dispatcher.BeginInvoke(() => UpdateCardWidth(), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void UpdateCardWidth()
    {
        if (ClothesList == null || ContentScroller == null) return;
        var masonry = FindVisualChild<MasonryPanel>(ClothesList);
        if (masonry == null) return;

        double availWidth = ContentScroller.ActualWidth - 128;
        if (availWidth <= 0) return;

        double gap = 24;
        int cols = (int)((availWidth + gap) / (280 + gap));
        cols = Math.Max(1, cols);

        double totalGap = gap * (cols - 1);
        double cardWidth = Math.Floor((availWidth - totalGap) / cols);
        cardWidth = Math.Clamp(cardWidth, 240, 320);

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
        _state.SetSearchText(TxtSearch.Text);
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

        EditorModal.Show(new ClothingEditorPanel(clothing), async result =>
        {
            if (result.Type == EditorResultType.Saved)
                await _clothingService.UpdateClothingAsync(result.Entity!);
            else if (result.Type == EditorResultType.Deleted)
                await _clothingService.DeleteClothingAsync(clothing.Id);
            await LoadClothesAsync();
        });
    }

    private async void ClothingCard_Delete(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not Clothing clothing) return;

        var result = MessageBox.Show(
            $"确定删除「{clothing.Name}」吗？",
            "删除衣服",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            await _clothingService.DeleteClothingAsync(clothing.Id);
            await LoadClothesAsync();
        }
    }
}
