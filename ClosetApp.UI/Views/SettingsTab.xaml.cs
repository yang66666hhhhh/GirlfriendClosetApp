using System.Windows;
using System.Windows.Controls;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class SettingsTab : UserControl
{
    private readonly ThemeService _themeService;
    private readonly SettingsViewModel _viewModel;

    public SettingsTab()
    {
        _themeService = App.Services.GetRequiredService<ThemeService>();
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += async (_, _) => await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            await _viewModel.InitializeAsync();
            AppearancePanel.Refresh();
            await ImageMaintenancePanel.RefreshAsync();
            await LogMaintenancePanel.RefreshAsync();
            await BackupPanel.RefreshAsync();
            await WeatherPreferencesPanel.RefreshAsync();
            await _viewModel.RefreshAiGenerationSettingsAsync();
            AiImageGenerationPanel.DataContext = _viewModel;
            await AiImageGenerationPanel.RefreshAsync();
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("设置页面刷新失败", $"无法加载最新设置数据：{ex.Message}");
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
}
