using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class AddOutfitPanel : UserControl
{
    private readonly IClothingService _clothingService;
    private readonly IOutfitService _outfitService;
    private readonly List<SelectableClothing> _allItems = new();

    public event Action? CloseRequested;
    public event Action? SaveCompleted;

    public AddOutfitPanel()
    {
        InitializeComponent();
        _clothingService = App.Services.GetRequiredService<IClothingService>();
        _outfitService = App.Services.GetRequiredService<IOutfitService>();
        CmbScene.SelectedIndex = 4;
        CmbSeason.SelectedIndex = 0;
        Loaded += async (s, e) => await LoadClothesAsync();
    }

    public async Task LoadDataAsync() => await LoadClothesAsync();

    private async Task LoadClothesAsync()
    {
        var clothes = await _clothingService.GetAllClothesAsync();
        _allItems.Clear();
        _allItems.AddRange(clothes.Select(c => new SelectableClothing(c)));

        DressList.ItemsSource = _allItems.Where(c => c.Clothing.Type == ClothingType.Dress).ToList();
        TopsList.ItemsSource = _allItems.Where(c => c.Clothing.Type is ClothingType.Top or ClothingType.Outerwear).ToList();
        BottomsList.ItemsSource = _allItems.Where(c => c.Clothing.Type is ClothingType.Bottom or ClothingType.Skirt).ToList();
        ShoesList.ItemsSource = _allItems.Where(c => c.Clothing.Type == ClothingType.Shoes).ToList();
        AccessoryList.ItemsSource = _allItems.Where(c => c.Clothing.Type == ClothingType.Accessory).ToList();
    }

    private void ClothItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not SelectableClothing item) return;

        if (item.IsSelected)
        {
            item.IsSelected = false;
            UpdateSectionStates();
            UpdatePreview();
            return;
        }

        var type = item.Clothing.Type;

        if (type == ClothingType.Dress)
        {
            ClearGroup(c => c.Clothing.Type is ClothingType.Top or ClothingType.Outerwear or ClothingType.Bottom or ClothingType.Skirt);
            SelectSingle(item, c => c.Clothing.Type == ClothingType.Dress);
        }
        else if (type is ClothingType.Top or ClothingType.Outerwear)
        {
            ClearGroup(c => c.Clothing.Type == ClothingType.Dress);
            SelectSingle(item, c => c.Clothing.Type is ClothingType.Top or ClothingType.Outerwear);
        }
        else if (type is ClothingType.Bottom or ClothingType.Skirt)
        {
            ClearGroup(c => c.Clothing.Type == ClothingType.Dress);
            SelectSingle(item, c => c.Clothing.Type is ClothingType.Bottom or ClothingType.Skirt);
        }
        else if (type == ClothingType.Shoes)
        {
            SelectSingle(item, c => c.Clothing.Type == ClothingType.Shoes);
        }
        else if (type == ClothingType.Accessory)
        {
            SelectSingle(item, c => c.Clothing.Type == ClothingType.Accessory);
        }

        UpdateSectionStates();
        UpdatePreview();
    }

    private void SelectSingle(SelectableClothing target, Func<SelectableClothing, bool> group)
    {
        foreach (var item in _allItems.Where(group))
            item.IsSelected = false;
        target.IsSelected = true;
    }

    private void ClearGroup(Func<SelectableClothing, bool> group)
    {
        foreach (var item in _allItems.Where(group))
            item.IsSelected = false;
    }

    private void UpdateSectionStates()
    {
        bool hasDress = _allItems.Any(i => i.IsSelected && i.Clothing.Type == ClothingType.Dress);
        bool hasTop = _allItems.Any(i => i.IsSelected && i.Clothing.Type is ClothingType.Top or ClothingType.Outerwear);
        bool hasBottom = _allItems.Any(i => i.IsSelected && i.Clothing.Type is ClothingType.Bottom or ClothingType.Skirt);

        TopSection.IsEnabled = !hasDress;
        BottomSection.IsEnabled = !hasDress;
        DressSection.IsEnabled = !hasTop && !hasBottom;

        double disabledOpacity = 0.4;
        TopSection.Opacity = hasDress ? disabledOpacity : 1;
        BottomSection.Opacity = hasDress ? disabledOpacity : 1;
        DressSection.Opacity = (hasTop || hasBottom) ? disabledOpacity : 1;
    }

    private void UpdatePreview()
    {
        var selected = _allItems.Where(i => i.IsSelected).ToList();

        if (selected.Count == 0)
        {
            EmptyPreview.Visibility = Visibility.Visible;
            PreviewCanvas.Visibility = Visibility.Collapsed;
            TxtPreviewCount.Text = "";
            return;
        }

        EmptyPreview.Visibility = Visibility.Collapsed;
        PreviewCanvas.Visibility = Visibility.Visible;
        TxtPreviewCount.Text = $"已选 {selected.Count} 件";
        PreviewCanvas.Children.Clear();

        double canvasWidth = 216;
        double y = 0;
        double gap = 4;

        var layers = new (Func<SelectableClothing, bool> Filter, double Scale)[]
        {
            (i => i.Clothing.Type == ClothingType.Dress, 0.80),
            (i => i.Clothing.Type is ClothingType.Top or ClothingType.Outerwear, 0.62),
            (i => i.Clothing.Type is ClothingType.Bottom or ClothingType.Skirt, 0.60),
            (i => i.Clothing.Type == ClothingType.Shoes, 0.40),
            (i => i.Clothing.Type == ClothingType.Accessory, 0.30),
        };

        foreach (var (filter, scale) in layers)
        {
            var item = selected.FirstOrDefault(filter);
            if (item == null) continue;

            double w = canvasWidth * scale;
            double h = w;
            if (item.Clothing.Type == ClothingType.Dress) h = w * 1.3;
            else if (item.Clothing.Type is ClothingType.Bottom or ClothingType.Skirt) h = w * 1.1;

            double x = (canvasWidth - w) / 2;

            var container = new Grid { Width = w, Height = h };

            var imageBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true,
                Background = Brushes.Transparent
            };
            imageBorder.Child = new Image
            {
                Source = LoadImage(item.Clothing.ImagePath),
                Stretch = Stretch.Uniform
            };
            container.Children.Add(imageBorder);

            var deleteBtn = new Button
            {
                Width = 20, Height = 20,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 3, 3, 0),
                Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 11,
                Cursor = Cursors.Hand,
                Visibility = Visibility.Collapsed,
                Content = "✕",
                Padding = new Thickness(0),
                Tag = item
            };
            deleteBtn.Click += PreviewItem_Delete;
            container.Children.Add(deleteBtn);

            var hoverArea = new Border
            {
                Child = container,
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            hoverArea.MouseEnter += (s, e) => deleteBtn.Visibility = Visibility.Visible;
            hoverArea.MouseLeave += (s, e) => deleteBtn.Visibility = Visibility.Collapsed;

            Canvas.SetLeft(hoverArea, x);
            Canvas.SetTop(hoverArea, y);
            PreviewCanvas.Children.Add(hoverArea);

            y += h + gap;
        }

        PreviewCanvas.Height = y;
    }

    private void PreviewItem_Delete(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SelectableClothing item)
        {
            item.IsSelected = false;
            UpdateSectionStates();
            UpdatePreview();
        }
    }

    private static ImageSource? LoadImage(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        try
        {
            string? resolved = null;
            if (System.IO.File.Exists(path)) resolved = path;
            else
            {
                var full = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                if (System.IO.File.Exists(full)) resolved = full;
                else
                {
                    var local = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ClosetApp", "images", path);
                    if (System.IO.File.Exists(local)) resolved = local;
                }
            }
            if (resolved == null) return null;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(resolved, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.DecodePixelWidth = 300;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("请输入搭配名称", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var selectedClothes = _allItems.Where(i => i.IsSelected).Select(i => i.Clothing).ToList();
        if (selectedClothes.Count == 0)
        {
            MessageBox.Show("请至少选择一件衣服", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var scene = (CmbScene.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "Work" => OutfitScene.Work, "Date" => OutfitScene.Date,
            "Travel" => OutfitScene.Travel, "Party" => OutfitScene.Party,
            _ => OutfitScene.Casual
        };
        var season = (CmbSeason.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "Spring" => Season.Spring, "Summer" => Season.Summer,
            "Autumn" => Season.Autumn, "Winter" => Season.Winter,
            _ => Season.AllSeason
        };

        var outfit = new Outfit
        {
            Name = TxtName.Text.Trim(),
            Scene = scene,
            Season = season,
            Rating = (int)RatingControl.Value
        };

        foreach (var clothing in selectedClothes)
            outfit.OutfitClothes.Add(new OutfitClothing { OutfitId = outfit.Id, ClothingId = clothing.Id });

        await _outfitService.AddOutfitAsync(outfit);

        TxtName.Text = string.Empty;
        foreach (var item in _allItems) item.IsSelected = false;
        UpdateSectionStates();
        UpdatePreview();

        SaveCompleted?.Invoke();
        CloseRequested?.Invoke();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();
    private void CancelButton_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();
}

public class SelectableClothing : System.ComponentModel.INotifyPropertyChanged
{
    public Clothing Clothing { get; }
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    }
    public string? ImagePath => Clothing.ImagePath;
    public SelectableClothing(Clothing clothing) => Clothing = clothing;
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}
