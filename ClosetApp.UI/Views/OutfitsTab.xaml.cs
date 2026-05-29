using System.Windows;
using System.Windows.Controls;
using ClosetApp.UI.Components.Outfit.Controls;
using ClosetApp.UI.Components.Outfit.Editor;
using ClosetApp.UI.Components.Shared;
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
            if (e.PropertyName is nameof(OutfitsViewModel.DisplayedOutfits) or nameof(OutfitsViewModel.IsEmpty))
            {
                AttachCardHandlers();
            }
        };
        Loaded += async (_, _) => await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            await _viewModel.LoadOutfitsAsync();
            AttachCardHandlers();
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("刷新搭配失败", ex.Message);
        }
    }

    private void AttachCardHandlers()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            foreach (var item in OutfitsList.Items)
            {
                var container = OutfitsList.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                if (container == null)
                    continue;

                var card = VisualTreeHelperExtensions.FindVisualChild<OutfitCard>(container);
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
        }), System.Windows.Threading.DispatcherPriority.Loaded);
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

    private async void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is not MainWindow window)
            return;

        await window.NavigateToSettingsAsync();
    }

}
