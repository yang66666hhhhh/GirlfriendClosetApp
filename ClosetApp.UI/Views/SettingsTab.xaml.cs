using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using ClosetApp.UI.Components.Shared;
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
            await RefreshStatsAsync();
            await RefreshBackupStateAsync();
            await _viewModel.RefreshWeatherAsync(showStatus: false);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("刷新设置失败", ex.Message);
        }
    }

    private static string GetVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version == null ? "开发版" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private async Task RefreshStatsAsync()
    {
        await _viewModel.RefreshStatsAsync();
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
        await RefreshStatsAsync();
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

    // ── 图片维护 ──

    private async void ClearThumbnails_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定清理图片缓存吗？原始图片不会被删除。",
            "清理缓存",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.OK)
            return;

        await _imageMaintenanceService.CleanupImageCacheAsync();
        await RefreshStatsAsync();
        MessageBox.Show("图片缓存已清理。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess("图片缓存已清理");
    }

    private async void RebuildThumbnails_Click(object sender, RoutedEventArgs e)
    {
        var result = await _imageMaintenanceService.RebuildMissingThumbnailsAsync();
        await RefreshStatsAsync();
        MessageBox.Show(result.Summary, "图片缓存", MessageBoxButton.OK, MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess("图片缓存已重建", result.Summary);
    }

    private async void CleanupOrphanOriginals_Click(object sender, RoutedEventArgs e)
    {
        var analysis = await _imageMaintenanceService.AnalyzeOrphanOriginalsAsync();
        if (!analysis.HasOrphans)
        {
            MessageBox.Show("没有发现可清理的孤儿原图。", "原图治理", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"发现 {analysis.OrphanCount} 张数据库未引用的原图，占用 {FileSizeFormatter.Format(analysis.TotalBytes)}。\n\n清理会同时删除这些原图对应的主视觉和小预览缓存，但不会删除任何仍被衣物引用的图片。确定继续吗？",
            "清理孤儿原图",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.OK)
            return;

        var result = await _imageMaintenanceService.CleanupOrphanOriginalsAsync();
        await RefreshStatsAsync();
        MessageBox.Show(result.Summary, "原图治理", MessageBoxButton.OK, MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess("孤儿原图已清理", result.Summary);
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
        await RefreshStatsAsync();
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

    private async void RepairMissingImages_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择旧图片所在目录，应用会按文件名尝试重连缺失图片。"
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FolderName))
            return;

        var repairedCount = await _imageMaintenanceService.RelinkMissingImagesAsync(dialog.FolderName);
        await RefreshStatsAsync();
        await RequestAppRefreshAsync(clothes: true, outfits: true);

        MessageBox.Show(
            repairedCount == 0
                ? "没有找到可重连的图片文件。"
                : $"已修复 {repairedCount} 张缺失图片。",
            "图片修复",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess(
            repairedCount == 0 ? "没有需要修复的图片" : "缺失图片已重连",
            repairedCount == 0 ? null : $"共修复 {repairedCount} 张图片。");
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