using System.Windows;
using System.Windows.Controls;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Views;

public partial class AddClothingDialog : Window
{
    public Clothing? Result { get; private set; }

    public AddClothingDialog()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("请输入衣服名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtName.Focus();
            return;
        }

        var type = ClothingType.Top;
        if (CmbType.SelectedItem is ComboBoxItem typeItem && typeItem.Tag is string typeStr)
        {
            type = typeStr switch
            {
                "Top" => ClothingType.Top,
                "Bottom" => ClothingType.Bottom,
                "Outerwear" => ClothingType.Outerwear,
                "Dress" => ClothingType.Dress,
                "Shoes" => ClothingType.Shoes,
                "Accessory" => ClothingType.Accessory,
                _ => ClothingType.Top
            };
        }

        var season = Season.AllSeason;
        if (CmbSeason.SelectedItem is ComboBoxItem seasonItem && seasonItem.Tag is string seasonStr)
        {
            season = seasonStr switch
            {
                "Spring" => Season.Spring,
                "Summer" => Season.Summer,
                "Autumn" => Season.Autumn,
                "Winter" => Season.Winter,
                _ => Season.AllSeason
            };
        }

        var favLevel = 3;
        if (CmbFavorite.SelectedItem is ComboBoxItem favItem && favItem.Tag is string favStr && int.TryParse(favStr, out var level))
        {
            favLevel = level;
        }

        Result = new Clothing
        {
            Name = TxtName.Text.Trim(),
            Type = type,
            Season = season,
            Color = string.IsNullOrWhiteSpace(TxtColor.Text) ? null : TxtColor.Text.Trim(),
            Brand = string.IsNullOrWhiteSpace(TxtBrand.Text) ? null : TxtBrand.Text.Trim(),
            FavoriteLevel = favLevel,
            IsFavorite = favLevel >= 4
        };

        DialogResult = true;
        Close();
    }
}
