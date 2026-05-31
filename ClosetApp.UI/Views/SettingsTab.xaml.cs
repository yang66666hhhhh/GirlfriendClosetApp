using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

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
            TxtDataDir.Text = AppPaths.BaseDir;
            TxtImagesDir.Text = AppPaths.ImagesDir;
            TxtLogDir.Text = AppPaths.LogsDir;
            TxtVersion.Text = $"版本 {GetVersion()}";
            await _viewModel.InitializeAsync();
            ApplyThemeCardSelection(_themeService.CurrentTheme);
            await ImageMaintenancePanel.RefreshAsync();
            await RefreshBackupStateAsync();
            await _viewModel.RefreshWeatherAsync(showStatus: false);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("设置页面刷新失败", $"无法加载最新设置数据：{ex.Message}");
        }
    }

    private static string GetVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version == null ? "开发版" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static void OpenPath(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private static void RevealFile(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Directory.CreateDirectory(directory);
        if (File.Exists(filePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
            return;
        }

        OpenPath(directory);
    }

    private void OpenDataDir_Click(object sender, RoutedEventArgs e) => OpenPath(AppPaths.BaseDir);
    private void OpenDatabase_Click(object sender, RoutedEventArgs e) => RevealFile(AppPaths.DatabasePath);
    private void OpenImagesDir_Click(object sender, RoutedEventArgs e) => OpenPath(AppPaths.ImagesDir);
    private void OpenThumbnailsDir_Click(object sender, RoutedEventArgs e) => OpenPath(AppPaths.ThumbnailsDir);
    private void OpenLogsDir_Click(object sender, RoutedEventArgs e) => OpenPath(AppPaths.LogsDir);
    private void OpenBackupsDir_Click(object sender, RoutedEventArgs e) => OpenPath(AppPaths.BackupsDir);
    private void OpenAppDir_Click(object sender, RoutedEventArgs e) => OpenPath(AppDomain.CurrentDomain.BaseDirectory);

    private async void RefreshStats_Click(object sender, RoutedEventArgs e)
    {
        await ImageMaintenancePanel.RefreshAsync();
        await RefreshBackupStateAsync();
        ToastService.Instance.ShowInfo("统计信息已刷新。");
    }

    private async void RefreshWeather_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshWeatherAsync(showStatus: true);
    }

    private async void SaveWeatherCity_Click(object sender, RoutedEventArgs e)
    {
        var city = _viewModel.WeatherCity.Trim();
        await _viewModel.SaveWeatherCityAsync(city);
        if (string.IsNullOrWhiteSpace(city))
            TxtWeatherCity.Focus();
        await RequestAppRefreshAsync(outfits: true);
    }

    private async void SaveRecommendationPreferences_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveRecommendationPreferencesAsync();
        await RequestAppRefreshAsync(outfits: true);
    }

    // ── 主题切换 ──

    private void ThemeCard_Selected(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Components.Shared.ThemeCard card)
            return;
        _ = ApplyThemeAsync(card.ThemeKind);
    }

    private async Task ApplyThemeAsync(AppThemeKind theme)
    {
        await _viewModel.ApplyThemeAsync(theme);
        ApplyThemeCardSelection(theme);
        ToastService.Instance.ShowSuccess(theme == AppThemeKind.Rose ? "已切换到柔粉主题" : "已切换到清蓝主题");
    }

    private void ApplyThemeCardSelection(AppThemeKind theme)
    {
        ThemeRoseCard.IsSelected = theme == AppThemeKind.Rose;
        ThemeBlueCard.IsSelected = theme == AppThemeKind.Blue;
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

    // ── 备份 ──

    private async void ExportBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "ZIP 备份包|*.zip|JSON 备份|*.json",
            DefaultExt = ".zip",
            FileName = Path.GetFileName(_viewModel.BuildDefaultBackupPath()),
            InitialDirectory = AppPaths.BackupsDir
        };

        if (dialog.ShowDialog() != true)
            return;

        var validation = await _viewModel.ValidateBackupExportAsync(dialog.FileName);
        if (!ConfirmExport(validation))
            return;

        var result = await _viewModel.ExportBackupWithFeedbackAsync(dialog.FileName);
        MessageBox.Show(BuildExportMessage(result), "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void QuickExportBackup_Click(object sender, RoutedEventArgs e)
    {
        var filePath = _viewModel.BuildDefaultBackupPath();
        var validation = await _viewModel.ValidateBackupExportAsync(filePath);
        if (!ConfirmExport(validation))
            return;

        var result = await _viewModel.ExportBackupWithFeedbackAsync(filePath);
        MessageBox.Show(BuildExportMessage(result), "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void ImportBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "备份文件|*.zip;*.json|ZIP 备份包|*.zip|JSON 备份|*.json",
            CheckFileExists = true,
            InitialDirectory = AppPaths.BackupsDir
        };

        if (dialog.ShowDialog() != true)
            return;

        var confirm = MessageBox.Show(
            "导入会覆盖当前数据库中的衣服、搭配、标签和穿着记录。ZIP 备份包会同时恢复图片，旧版 JSON 只恢复核心数据，确定继续吗？",
            "确认导入备份",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.OK)
            return;

        var result = await _viewModel.ImportBackupWithFeedbackAsync(dialog.FileName);
        await RequestAppRefreshAsync(clothes: true, outfits: true, tags: true);
        MessageBox.Show(BuildImportMessage(result), "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void RefreshBackupState_Click(object sender, RoutedEventArgs e)
    {
        await RefreshBackupStateAsync();
        ToastService.Instance.ShowInfo("备份状态已刷新。");
    }

    private async void ClearBackupHistory_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "确定清空备份历史吗？这不会删除已经导出的备份文件。",
            "清空备份历史",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.OK)
            return;

        await _viewModel.ClearBackupHistoryWithFeedbackAsync();
    }

    private async void RepairMissingImages_Click(object sender, RoutedEventArgs e)
    {
        await ImageMaintenancePanel.RepairMissingImagesAsync();
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

    private void OpenBackupFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string filePath } || string.IsNullOrWhiteSpace(filePath))
            return;
        RevealFile(filePath);
    }

    private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string filePath } || string.IsNullOrWhiteSpace(filePath))
            return;
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            OpenPath(directory);
    }

    private async Task RefreshBackupStateAsync(BackupImportResult? latestImport = null)
    {
        await _viewModel.RefreshBackupStateAsync(latestImport);
    }

    private static bool ConfirmExport(BackupValidationResult validation)
    {
        if (!validation.HasWarnings)
            return true;

        var message = "导出前提醒：\n\n" + string.Join("\n", validation.Warnings) + "\n\n确定继续导出吗？";
        return MessageBox.Show(
            message,
            "确认导出备份",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning) == MessageBoxResult.OK;
    }

    private static string BuildExportMessage(BackupExportResult result)
    {
        var message = $"{result.Summary}\n文件位置：{result.FilePath}";
        if (result.Warnings.Count > 0)
            message += $"\n\n提醒：{string.Join(" ", result.Warnings)}";
        return message;
    }

    private static string BuildImportMessage(BackupImportResult result)
    {
        var message = result.Summary;
        if (result.Warnings.Count > 0)
            message += $"\n\n提醒：{string.Join(" ", result.Warnings)}";
        return message;
    }
}
