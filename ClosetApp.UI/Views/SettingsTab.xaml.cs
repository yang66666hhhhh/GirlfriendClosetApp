using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class SettingsTab : UserControl
{
    private readonly IImageMaintenanceService _imageMaintenanceService;
    private readonly ThemeService _themeService;
    private readonly SettingsViewModel _viewModel;

    public SettingsTab()
    {
        _imageMaintenanceService = App.Services.GetRequiredService<IImageMaintenanceService>();
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
            TxtLogDir.Text = AppPaths.LogsDir;
            await _viewModel.InitializeAsync();
            AppearancePanel.Refresh();
            await ImageMaintenancePanel.RefreshAsync();
            await BackupPanel.RefreshAsync();
            await WeatherPreferencesPanel.RefreshAsync();
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("设置页面刷新失败", $"无法加载最新设置数据：{ex.Message}");
        }
    }

    private static void OpenPath(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private void OpenLogsDir_Click(object sender, RoutedEventArgs e) => OpenPath(AppPaths.LogsDir);

    private async void RefreshStats_Click(object sender, RoutedEventArgs e)
    {
        await ImageMaintenancePanel.RefreshAsync();
        await BackupPanel.RefreshAsync();
        ToastService.Instance.ShowInfo("统计信息已刷新。");
    }

    private async Task ApplyThemeAsync(AppThemeKind theme)
    {
        await _viewModel.ApplyThemeAsync(theme);
        AppearancePanel.ApplyThemeCardSelection(theme);
        ToastService.Instance.ShowSuccess(theme == AppThemeKind.Rose ? "已切换到柔粉主题" : "已切换到清蓝主题");
    }

    private async void ClearLogs_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定清理历史日志吗？今天正在写入的日志会保留。",
            "清理日志",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.OK)
            return;

        await _imageMaintenanceService.CleanupLogsAsync();
        await ImageMaintenancePanel.RefreshAsync();
        MessageBox.Show("历史日志已清理。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess("历史日志已清理");
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
