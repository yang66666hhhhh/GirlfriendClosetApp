using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class OutfitsTab : UserControl
{
    private readonly IOutfitService _outfitService;

    public OutfitsTab()
    {
        InitializeComponent();
        _outfitService = App.Services.GetRequiredService<IOutfitService>();
        Loaded += async (s, e) => await LoadOutfitsAsync();
    }

    private async Task LoadOutfitsAsync()
    {
        var outfits = await _outfitService.GetAllOutfitsAsync();
        var list = outfits.ToList();
        OutfitsList.ItemsSource = list;
        TxtEmpty.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CreateOutfit_Click(object sender, RoutedEventArgs e)
    {
        var panel = new AddOutfitPanel();
        panel.SaveCompleted += async () => await LoadOutfitsAsync();
        panel.CloseRequested += () => ModalService.Instance.Hide();
        ModalService.Instance.Show(panel);
    }

    private async void EditOutfit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Outfit outfit)
        {
            var dialog = new EditOutfitDialog(outfit);
            if (dialog.ShowDialog() == true)
            {
                await LoadOutfitsAsync();
            }
        }
    }

    private async void DeleteOutfit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Outfit outfit)
        {
            var result = MessageBox.Show(
                $"确定删除搭配「{outfit.Name}」吗？",
                "删除搭配",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await _outfitService.DeleteOutfitAsync(outfit.Id);
                await LoadOutfitsAsync();
            }
        }
    }
}
