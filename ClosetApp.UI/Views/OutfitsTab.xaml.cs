using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Outfit.Controls;
using ClosetApp.UI.Components.Outfit.Editor;
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

    private void OutfitsList_Loaded(object sender, RoutedEventArgs e)
    {
        if (OutfitsList.ItemsSource is IEnumerable<Outfit> outfits)
        {
            foreach (Outfit outfit in outfits)
            {
                var container = OutfitsList.ItemContainerGenerator.ContainerFromItem(outfit) as ContentPresenter;
                if (container != null)
                {
                    var card = FindVisualChild<OutfitCard>(container);
                    if (card != null)
                    {
                        card.Outfit = outfit;
                        card.EditClicked += OutfitCard_EditClicked;
                        card.DeleteClicked += OutfitCard_DeleteClicked;
                    }
                }
            }
        }
    }

    private void CreateOutfit_Click(object sender, RoutedEventArgs e)
    {
        var panel = new OutfitEditorPanel();
        panel.SaveCompleted += async () => await LoadOutfitsAsync();
        panel.CloseRequested += () => ModalService.Instance.Hide();
        ModalService.Instance.Show(panel);
    }

    private void OutfitCard_EditClicked(object sender, RoutedEventArgs e)
    {
        if (sender is OutfitCard card && card.Outfit != null)
        {
            var panel = new OutfitEditorPanel(card.Outfit);
            panel.SaveCompleted += async () => await LoadOutfitsAsync();
            panel.CloseRequested += () => ModalService.Instance.Hide();
            ModalService.Instance.Show(panel);
        }
    }

    private async void OutfitCard_DeleteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is OutfitCard card && card.Outfit != null)
        {
            var result = MessageBox.Show(
                $"确定删除搭配「{card.Outfit.Name}」吗？",
                "删除搭配",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await _outfitService.DeleteOutfitAsync(card.Outfit.Id);
                await LoadOutfitsAsync();
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }
}
