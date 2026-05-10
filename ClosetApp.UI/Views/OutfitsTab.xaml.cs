using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Outfit.Controls;
using ClosetApp.UI.Components.Outfit.Editor;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using OutfitEntity = ClosetApp.Domain.Entities.Outfit;

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

        _ = Dispatcher.BeginInvoke(() =>
        {
            foreach (var item in OutfitsList.Items)
            {
                var container = OutfitsList.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                if (container != null)
                {
                    var card = FindVisualChild<OutfitCard>(container);
                    if (card != null)
                    {
                        card.EditCompleted -= OutfitCard_EditCompleted;
                        card.DeleteRequested -= OutfitCard_DeleteRequested;
                        card.EditCompleted += OutfitCard_EditCompleted;
                        card.DeleteRequested += OutfitCard_DeleteRequested;
                    }
                }
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void CreateOutfit_Click(object sender, RoutedEventArgs e)
    {
        var panel = new OutfitEditorPanel();
        panel.SaveCompleted += async () => await LoadOutfitsAsync();
        panel.CloseRequested += () => ModalService.Instance.Hide();
        ModalService.Instance.Show(panel);
    }

    private async void OutfitCard_EditCompleted(object? sender, OutfitEntity outfit)
    {
        await LoadOutfitsAsync();
    }

    private async void OutfitCard_DeleteRequested(object? sender, OutfitEntity outfit)
    {
        await _outfitService.DeleteOutfitAsync(outfit.Id);
        await LoadOutfitsAsync();
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
