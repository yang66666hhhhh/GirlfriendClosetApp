using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class ClothesTab : UserControl
{
    private readonly IClothingService _clothingService;
    private List<Clothing> _allClothes = new();
    private List<Clothing> _filteredClothes = new();
    private ClothingType? _selectedCategory;

    public ClothesTab()
    {
        InitializeComponent();
        _clothingService = App.Services.GetRequiredService<IClothingService>();
        Loaded += (s, e) => _ = LoadClothesAsync();
    }

    private async Task LoadClothesAsync()
    {
        var clothes = await _clothingService.GetAllClothesAsync();
        _allClothes = clothes.ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filteredClothes = _selectedCategory == null
            ? _allClothes
            : _allClothes.Where(c => c.Type == _selectedCategory).ToList();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (TxtCount == null) return;

        if (_allClothes.Count == 0)
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

            TxtCount.Text = _selectedCategory == null
                ? $"{_filteredClothes.Count} 件"
                : $"{_filteredClothes.Count} 件";

            ClothesList.ItemsSource = _filteredClothes;

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
        _selectedCategory = rb.Name switch
        {
            "ChipAll" => null,
            "ChipTop" => ClothingType.Top,
            "ChipBottom" => ClothingType.Bottom,
            "ChipDress" => ClothingType.Dress,
            "ChipOuter" => ClothingType.Outerwear,
            "ChipShoes" => ClothingType.Shoes,
            "ChipAccessory" => ClothingType.Accessory,
            _ => null
        };
        ApplyFilter();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = TxtSearch.Text.Trim().ToLower();
        _filteredClothes = string.IsNullOrEmpty(query)
            ? (_selectedCategory == null ? _allClothes : _allClothes.Where(c => c.Type == _selectedCategory).ToList())
            : _allClothes.Where(c =>
                (_selectedCategory == null || c.Type == _selectedCategory) &&
                (c.Name.ToLower().Contains(query) ||
                 c.Type.ToString()!.ToLower().Contains(query) ||
                 c.Season.ToString()!.ToLower().Contains(query) ||
                 (c.Color?.ToLower() ?? "").Contains(query))
            ).ToList();

        TxtCount.Text = $"{_filteredClothes.Count} 件";
        ClothesList.ItemsSource = _filteredClothes;
        Dispatcher.BeginInvoke(() => UpdateCardWidth(), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void AddClothing_Click(object sender, RoutedEventArgs e)
    {
        var panel = new AddClothingPanel();
        panel.Saved += async (_, clothing) =>
        {
            await _clothingService.AddClothingAsync(clothing);
            ModalService.Instance.Hide();
            await LoadClothesAsync();
        };
        panel.Cancelled += (_, _) => ModalService.Instance.Hide();
        ModalService.Instance.Show(panel);
    }

    private async void ClothingCard_Edit(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not Clothing clothing) return;

        var dialog = new EditClothingDialog(clothing);
        if (dialog.ShowDialog() == true)
            await LoadClothesAsync();
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
