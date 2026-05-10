using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Views;

public partial class EditClothingDialog : Window
{
    private readonly Clothing _clothing;
    private readonly IClothingService _clothingService;

    private static readonly string ImageFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClosetApp", "images");

    public EditClothingDialog(Clothing clothing)
    {
        InitializeComponent();
        _clothing = clothing;
        _clothingService = App.Services.GetRequiredService<IClothingService>();
        LoadData();
    }

    private void LoadData()
    {
        TxtName.Text = _clothing.Name;
        TxtColor.Text = _clothing.Color ?? "";
        TxtBrand.Text = _clothing.Brand ?? "";
        TxtNotes.Text = _clothing.Notes ?? "";
        TxtImagePath.Text = _clothing.ImagePath ?? "";
        RatingControl.Value = _clothing.FavoriteLevel;

        CmbType.SelectedIndex = _clothing.Type switch
        {
            ClothingType.Top => 0,
            ClothingType.Bottom => 1,
            ClothingType.Outerwear => 2,
            ClothingType.Dress => 3,
            ClothingType.Skirt => 4,
            ClothingType.Shoes => 5,
            ClothingType.Accessory => 6,
            _ => 0
        };

        CmbSeason.SelectedIndex = _clothing.Season switch
        {
            Season.Spring => 0,
            Season.Summer => 1,
            Season.Autumn => 2,
            Season.Winter => 3,
            Season.AllSeason => 4,
            _ => 4
        };

        LoadPreviewImage();
    }

    private void LoadPreviewImage()
    {
        var path = _clothing.ImagePath;
        if (string.IsNullOrEmpty(path))
        {
            PreviewImage.Source = null;
            TxtNoImage.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            string? resolved = null;
            if (File.Exists(path)) resolved = path;
            else
            {
                var full = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                if (File.Exists(full)) resolved = full;
                else
                {
                    var local = Path.Combine(ImageFolder, path);
                    if (File.Exists(local)) resolved = local;
                }
            }

            if (resolved != null)
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(resolved, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.DecodePixelWidth = 500;
                bmp.EndInit();
                bmp.Freeze();
                PreviewImage.Source = bmp;
                TxtNoImage.Visibility = Visibility.Collapsed;
            }
            else
            {
                PreviewImage.Source = null;
                TxtNoImage.Visibility = Visibility.Visible;
            }
        }
        catch
        {
            PreviewImage.Source = null;
            TxtNoImage.Visibility = Visibility.Visible;
        }
    }

    private void SelectImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.bmp"
        };
        if (dialog.ShowDialog() == true)
        {
            TxtImagePath.Text = dialog.FileName;
            _clothing.ImagePath = dialog.FileName;
            LoadPreviewImage();
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("请输入衣服名称", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _clothing.Name = TxtName.Text.Trim();
        _clothing.Type = (CmbType.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "Top" => ClothingType.Top,
            "Bottom" => ClothingType.Bottom,
            "Outerwear" => ClothingType.Outerwear,
            "Dress" => ClothingType.Dress,
            "Skirt" => ClothingType.Skirt,
            "Shoes" => ClothingType.Shoes,
            "Accessory" => ClothingType.Accessory,
            _ => ClothingType.Top
        };
        _clothing.Season = (CmbSeason.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "Spring" => Season.Spring,
            "Summer" => Season.Summer,
            "Autumn" => Season.Autumn,
            "Winter" => Season.Winter,
            _ => Season.AllSeason
        };
        _clothing.Color = string.IsNullOrWhiteSpace(TxtColor.Text) ? null : TxtColor.Text.Trim();
        _clothing.Brand = string.IsNullOrWhiteSpace(TxtBrand.Text) ? null : TxtBrand.Text.Trim();
        _clothing.Notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim();
        _clothing.ImagePath = string.IsNullOrWhiteSpace(TxtImagePath.Text) ? null : TxtImagePath.Text.Trim();
        _clothing.FavoriteLevel = (int)RatingControl.Value;
        _clothing.IsFavorite = _clothing.FavoriteLevel >= 4;

        await _clothingService.UpdateClothingAsync(_clothing);
        DialogResult = true;
        Close();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"确定删除「{_clothing.Name}」吗？",
            "删除衣服",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            await _clothingService.DeleteClothingAsync(_clothing.Id);
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
