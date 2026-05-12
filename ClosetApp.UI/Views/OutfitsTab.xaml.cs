using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Outfit.Controls;
using ClosetApp.UI.Components.Outfit.Editor;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.States;
using Microsoft.Extensions.DependencyInjection;
using OutfitEntity = ClosetApp.Domain.Entities.Outfit;

namespace ClosetApp.UI.Views;

public partial class OutfitsTab : UserControl
{
    private readonly IOutfitService _outfitService;
    private readonly OutfitsTabState _state = new();

    public OutfitsTab()
    {
        InitializeComponent();
        _outfitService = App.Services.GetRequiredService<IOutfitService>();
        Loaded += async (s, e) => await LoadOutfitsAsync();
    }

    private async Task LoadOutfitsAsync()
    {
        _state.BeginLoad();
        var outfits = await _outfitService.GetAllOutfitsAsync();
        _state.SetOutfits(outfits);
        OutfitsList.ItemsSource = _state.Outfits;
        TxtEmpty.Visibility = _state.IsEmpty ? Visibility.Visible : Visibility.Collapsed;

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
                        card.WornRequested -= OutfitCard_WornRequested;
                        card.EditCompleted += OutfitCard_EditCompleted;
                        card.DeleteRequested += OutfitCard_DeleteRequested;
                        card.WornRequested += OutfitCard_WornRequested;
                    }
                }
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void CreateOutfit_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new OutfitEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
                await LoadOutfitsAsync();
        });
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

    private async void OutfitCard_WornRequested(object? sender, OutfitEntity outfit)
    {
        await _outfitService.RecordWornDateAsync(outfit.Id, DateTime.Now);
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
