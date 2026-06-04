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
    private Task? _refreshTask;

    public OutfitsTab()
    {
        _viewModel = App.Services.GetRequiredService<OutfitsViewModel>();
        InitializeComponent();
        DataContext = _viewModel;
    }

    public Task RefreshAsync()
    {
        _refreshTask ??= RefreshCoreAsync();
        return _refreshTask;
    }

    private async Task RefreshCoreAsync()
    {
        try
        {
            await _viewModel.LoadOutfitsAsync();
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("搭配列表刷新失败", $"无法加载最新搭配数据：{ex.Message}");
        }
        finally
        {
            _refreshTask = null;
        }
    }

    private void CreateOutfit_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new OutfitEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved && result.Entity != null)
            {
                await _viewModel.RefreshAfterOutfitSavedWithFeedbackAsync(
                    result.Entity.Id,
                    "已保存搭配",
                    "新的搭配已经出现在列表里。");
                _viewModel.SelectOutfit(result.Entity);
            }
        });
    }

    private void OutfitCard_CardClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not OutfitCard { Outfit: { } outfit })
            return;

        ModalService.Instance.Show(new OutfitWorkspaceDialog(outfit));
    }

    private void OutfitCard_EditClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not OutfitCard { Outfit: { } outfit })
            return;

        OpenOutfitEditor(outfit);
    }

    private async void OutfitCard_DeleteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not OutfitCard { Outfit: { } outfit })
            return;

        await _viewModel.DeleteOutfitWithFeedbackAsync(outfit);
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

    private void OpenOutfitEditor(OutfitEntity outfit)
    {
        EditorModal.Show(new OutfitEditorPanel(outfit), async result =>
        {
            if (result.Type == EditorResultType.Saved && result.Entity != null)
            {
                await _viewModel.RefreshAfterOutfitSavedWithFeedbackAsync(
                    result.Entity.Id,
                    $"已更新「{result.Entity.Name}」",
                    "修改后的搭配已经同步到列表。");
                _viewModel.SelectOutfit(result.Entity);
            }
        });
    }

}
