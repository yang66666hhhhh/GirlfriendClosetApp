using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class EditOutfitDialog : Window
{
    private readonly Outfit _outfit;
    private readonly IClothingService _clothingService;
    private readonly IOutfitService _outfitService;
    private readonly List<Clothing> _selectedClothes = new();
    private List<Clothing> _allClothes = new();

    public EditOutfitDialog(Outfit outfit)
    {
        InitializeComponent();
        _outfit = outfit;
        _clothingService = App.Services.GetRequiredService<IClothingService>();
        _outfitService = App.Services.GetRequiredService<IOutfitService>();
        LoadData();
    }

    private async void LoadData()
    {
        TxtName.Text = _outfit.Name;
        RatingControl.Value = _outfit.Rating;

        var sceneIndex = _outfit.Scene switch
        {
            OutfitScene.Work => 0,
            OutfitScene.Date => 1,
            OutfitScene.Travel => 2,
            OutfitScene.Party => 3,
            OutfitScene.Casual => 4,
            _ => 4
        };
        CmbScene.SelectedIndex = sceneIndex;

        var seasonIndex = _outfit.Season switch
        {
            Season.Spring => 0,
            Season.Summer => 1,
            Season.Autumn => 2,
            Season.Winter => 3,
            Season.AllSeason => 4,
            _ => 4
        };
        CmbSeason.SelectedIndex = seasonIndex;

        var clothes = await _clothingService.GetAllClothesAsync();
        _allClothes = clothes.ToList();

        var selectedIds = _outfit.OutfitClothes.Select(oc => oc.ClothingId).ToHashSet();

        ClothesSelection.Items.Clear();
        foreach (var clothing in _allClothes)
        {
            var isSelected = selectedIds.Contains(clothing.Id);
            if (isSelected) _selectedClothes.Add(clothing);

            var border = new Border
            {
                Width = 80,
                Height = 100,
                Margin = new Thickness(4),
                Background = isSelected
                    ? new SolidColorBrush(Color.FromRgb(240, 238, 255))
                    : new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                CornerRadius = new CornerRadius(8),
                Cursor = Cursors.Hand,
                Tag = clothing
            };

            var grid = new Grid();
            grid.Children.Add(new Image
            {
                Source = LoadImage(clothing.ImagePath),
                Stretch = Stretch.UniformToFill
            });
            grid.Children.Add(new Border
            {
                Width = 18,
                Height = 18,
                CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush(Color.FromRgb(102, 126, 234)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 4, 0),
                Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed,
                Child = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"),
                    Fill = Brushes.White,
                    Stretch = Stretch.Uniform,
                    Width = 10,
                    Height = 10
                }
            });

            border.Child = grid;
            border.MouseLeftButtonDown += (s, e) => ToggleClothing(clothing, border);
            ClothesSelection.Items.Add(border);
        }

        UpdatePreview();
    }

    private ImageSource? LoadImage(string? path)
    {
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            return null;
        try
        {
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }
        catch { return null; }
    }

    private void ToggleClothing(Clothing clothing, Border border)
    {
        var isSelected = _selectedClothes.Contains(clothing);
        if (isSelected)
        {
            _selectedClothes.Remove(clothing);
            border.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            (border.Child as Grid)?.Children.OfType<Border>().FirstOrDefault()!.Visibility = Visibility.Collapsed;
        }
        else
        {
            _selectedClothes.Add(clothing);
            border.Background = new SolidColorBrush(Color.FromRgb(240, 238, 255));
            (border.Child as Grid)?.Children.OfType<Border>().FirstOrDefault()!.Visibility = Visibility.Visible;
        }
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (_selectedClothes.Count == 0)
        {
            EmptyPreview.Visibility = Visibility.Visible;
            PreviewItems.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyPreview.Visibility = Visibility.Collapsed;
            PreviewItems.Visibility = Visibility.Visible;
            PreviewItems.Items.Clear();
            foreach (var clothing in _selectedClothes)
            {
                var border = new Border { Height = 60, Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };
                border.Child = new Image { Source = LoadImage(clothing.ImagePath), Stretch = Stretch.Uniform };
                PreviewItems.Items.Add(border);
            }
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("请输入搭配名称", "提示");
            return;
        }

        _outfit.Name = TxtName.Text;
        _outfit.Scene = (CmbScene.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "Work" => OutfitScene.Work,
            "Date" => OutfitScene.Date,
            "Travel" => OutfitScene.Travel,
            "Party" => OutfitScene.Party,
            "Casual" => OutfitScene.Casual,
            _ => OutfitScene.Casual
        };
        _outfit.Season = (CmbSeason.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "Spring" => Season.Spring,
            "Summer" => Season.Summer,
            "Autumn" => Season.Autumn,
            "Winter" => Season.Winter,
            "AllSeason" => Season.AllSeason,
            _ => Season.AllSeason
        };
        _outfit.Rating = (int)RatingControl.Value;

        _outfit.OutfitClothes.Clear();
        foreach (var clothing in _selectedClothes)
        {
            _outfit.OutfitClothes.Add(new OutfitClothing
            {
                OutfitId = _outfit.Id,
                ClothingId = clothing.Id
            });
        }

        await _outfitService.UpdateOutfitAsync(_outfit);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}