using System.Windows;
using System.Windows.Controls;
using ClosetApp.UI.Components.Outfit.Controls;
using ClosetApp.UI.Components.Outfit.Editor;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using OutfitEntity = ClosetApp.Domain.Entities.Outfit;

namespace ClosetApp.UI.Views;

public partial class OutfitsTab : UserControl
{
    private readonly OutfitsViewModel _viewModel;

    public OutfitsTab()
    {
        _viewModel = App.Services.GetRequiredService<OutfitsViewModel>();
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(OutfitsViewModel.Outfits) or nameof(OutfitsViewModel.IsEmpty))
            {
                _ = Dispatcher.BeginInvoke(AttachCardHandlers, System.Windows.Threading.DispatcherPriority.Loaded);
            }
        };
        Loaded += async (_, _) => await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            await _viewModel.LoadOutfitsAsync();
            _ = Dispatcher.BeginInvoke(AttachCardHandlers, System.Windows.Threading.DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("刷新搭配失败", ex.Message);
        }
    }

    private void AttachCardHandlers()
    {
        foreach (var item in OutfitsList.Items)
        {
            var container = OutfitsList.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
            if (container == null)
                continue;

            var card = FindVisualChild<OutfitCard>(container);
            if (card == null)
                continue;

            card.EditCompleted -= OutfitCard_EditCompleted;
            card.DeleteRequested -= OutfitCard_DeleteRequested;
            card.WornRequested -= OutfitCard_WornRequested;
            card.FavoriteToggled -= OutfitCard_FavoriteToggled;
            card.EditCompleted += OutfitCard_EditCompleted;
            card.DeleteRequested += OutfitCard_DeleteRequested;
            card.WornRequested += OutfitCard_WornRequested;
            card.FavoriteToggled += OutfitCard_FavoriteToggled;
        }
    }

    private void CreateOutfit_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new OutfitEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
            {
                await _viewModel.RefreshAfterOutfitSavedWithFeedbackAsync(
                    "已保存搭配",
                    "新的搭配已经出现在列表里。");
            }
        });
    }

    private async void OutfitCard_EditCompleted(object? sender, OutfitEntity outfit)
    {
        await _viewModel.RefreshAfterOutfitSavedWithFeedbackAsync(
            $"已更新「{outfit.Name}」",
            "修改后的搭配已经同步到列表。");
    }

    private async void OutfitCard_DeleteRequested(object? sender, OutfitEntity outfit)
    {
        await _viewModel.DeleteOutfitWithFeedbackAsync(outfit);
    }

    private async void OutfitCard_WornRequested(object? sender, OutfitEntity outfit)
    {
        await _viewModel.RecordOutfitWornWithFeedbackAsync(outfit, outfit.Name);
    }

    private async void OutfitCard_FavoriteToggled(object? sender, RoutedEventArgs e)
    {
        if (sender is not OutfitCard card || card.Outfit == null) return;

        var isFav = await _viewModel.ToggleFavoriteWithFeedbackAsync(card.Outfit);
        if (isFav != null)
            card.ApplyFavoriteVisual(card.Outfit);
    }

    private void TodayHeroPrimaryAction_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.PrimaryWeatherRecommendation == null)
            CreateOutfit_Click(sender, e);
    }

    private void OpenOutfitHistory_Click(object sender, RoutedEventArgs e)
    {
        ModalService.Instance.Show(new OutfitHistoryDialog(_viewModel));
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is not MainWindow window)
            return;

        window.NavigateToSettings();
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
