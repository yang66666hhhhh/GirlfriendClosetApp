using System.Windows;
using System.Windows.Controls;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using ClosetApp.UI.Logic.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class SettingsTab : UserControl
{
    private readonly SettingsViewModel _viewModel;
    private readonly AppStartupCoordinator _startupCoordinator;
    private Task? _refreshTask;

    public SettingsTab()
    {
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
        _startupCoordinator = App.Services.GetRequiredService<AppStartupCoordinator>();
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
            await _startupCoordinator.WaitUntilReadyAsync();
            await _viewModel.InitializeAsync();
            AppearancePanel.Refresh();
            await ImageMaintenancePanel.RefreshAsync();
            await LogMaintenancePanel.RefreshAsync();
            await BackupPanel.RefreshAsync();
            await WeatherPreferencesPanel.RefreshAsync();
            await AiImageGenerationPanel.RefreshAsync();
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("设置页面刷新失败", $"无法加载最新设置数据：{ex.Message}");
        }
        finally
        {
            _refreshTask = null;
        }
    }

    private async Task ApplyThemeAsync(AppThemeKind theme)
    {
        await _viewModel.ApplyThemeAsync(theme);
        AppearancePanel.ApplyThemeCardSelection(theme);
        ToastService.Instance.ShowSuccess(theme == AppThemeKind.Rose ? "已切换到柔粉主题" : "已切换到清蓝主题");
    }

    private async Task RequestAppRefreshAsync(
        bool clothes = false,
        bool outfits = false,
        bool tags = false,
        bool settings = false)
    {
        if (Window.GetWindow(this) is not MainWindow window)
            return;
        await window.RefreshDataTabsAsync(clothes, outfits, tags, settings);
    }

    private async void ImageMaintenancePanel_WardrobeImagesChanged(object sender, EventArgs e)
    {
        await RequestAppRefreshAsync(clothes: true, outfits: true);
    }

    private async void BackupPanel_BackupImported(object sender, EventArgs e)
    {
        await RequestAppRefreshAsync(clothes: true, outfits: true, tags: true);
    }

    private async void BackupPanel_RepairMissingImagesRequested(object sender, EventArgs e)
    {
        await ImageMaintenancePanel.RepairMissingImagesAsync();
    }

    private async void WeatherPreferencesPanel_OutfitsRefreshRequested(object sender, EventArgs e)
    {
        await RequestAppRefreshAsync(outfits: true);
    }

    private async void AppearancePanel_ThemeChanged(object sender, AppThemeKind e)
    {
        await ApplyThemeAsync(e);
    }

    private async void AppearancePanel_OutfitCardDisplayModeChanged(object sender, OutfitCardDisplayMode e)
    {
        await _viewModel.SaveOutfitCardDisplayModeAsync(e);
        await RequestAppRefreshAsync(outfits: true);
    }

    private async void AppearancePanel_FontSizeLevelChanged(object sender, AppFontSizeLevel e)
    {
        await _viewModel.SaveFontSizeLevelAsync(e);
        AppearancePanel.ApplyFontSizeSelection(e);
    }

    private void OverviewThemeShortcut_Click(object sender, RoutedEventArgs e)
    {
        AppearancePanel.BringIntoView();
    }

    private void OverviewAiShortcut_Click(object sender, RoutedEventArgs e)
    {
        AiImageGenerationPanel.BringIntoView();
    }
}
