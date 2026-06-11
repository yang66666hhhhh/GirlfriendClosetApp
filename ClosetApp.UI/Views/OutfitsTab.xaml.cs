using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ClosetApp.UI.Components;
using ClosetApp.UI.Components.Outfit.Controls;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Components.Outfit.Editor;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using OutfitEntity = ClosetApp.Domain.Entities.Outfit;

namespace ClosetApp.UI.Views;

public partial class OutfitsTab : UserControl
{
    private readonly OutfitsViewModel _viewModel;
    private readonly AppStartupCoordinator _startupCoordinator;
    private Task? _refreshTask;

    public OutfitsTab()
    {
        _viewModel = App.Services.GetRequiredService<OutfitsViewModel>();
        _startupCoordinator = App.Services.GetRequiredService<AppStartupCoordinator>();
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
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
            await _startupCoordinator.WaitUntilReadyAsync();
            await _viewModel.LoadOutfitsAsync();
            ApplyDisplayModeSelection();
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

    private async void DisplayMode_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radioButton || radioButton.IsChecked != true)
            return;

        var mode = ReferenceEquals(radioButton, DisplayModeEffectImageFirst)
            ? OutfitCardDisplayMode.EffectImageFirst
            : OutfitCardDisplayMode.OutfitFirst;

        await _viewModel.SetCardDisplayModeAsync(mode);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OutfitsViewModel.CardDisplayMode))
            HandleCardDisplayModeChanged();
    }

    private void HandleCardDisplayModeChanged()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(HandleCardDisplayModeChanged, DispatcherPriority.Background);
            return;
        }

        ApplyDisplayModeSelection();
        RefreshOutfitsMasonryLayout();
    }

    private void ApplyDisplayModeSelection()
    {
        DisplayModeOutfitFirst.IsChecked = _viewModel.CardDisplayMode == OutfitCardDisplayMode.OutfitFirst;
        DisplayModeEffectImageFirst.IsChecked = _viewModel.CardDisplayMode == OutfitCardDisplayMode.EffectImageFirst;
    }

    private void RefreshOutfitsMasonryLayout()
    {
        Dispatcher.InvokeAsync(() =>
        {
            var masonry = VisualTreeHelperExtensions.FindVisualChild<MasonryPanel>(OutfitsList);
            if (masonry == null)
                return;

            foreach (UIElement child in masonry.Children)
                child.InvalidateMeasure();

            masonry.InvalidateMeasure();
            masonry.InvalidateArrange();
            OutfitsList.InvalidateMeasure();
        }, DispatcherPriority.Background);
    }

}
