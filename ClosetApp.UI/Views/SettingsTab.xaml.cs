using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Views;

public partial class SettingsTab : UserControl
{
    private readonly IBackupService _backupService;
    private readonly IImageMaintenanceService _imageMaintenanceService;

    public SettingsTab()
    {
        _backupService = App.Services.GetRequiredService<IBackupService>();
        _imageMaintenanceService = App.Services.GetRequiredService<IImageMaintenanceService>();
        InitializeComponent();
        Loaded += async (_, _) => await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        TxtDataDir.Text = AppPaths.BaseDir;
        TxtImagesDir.Text = AppPaths.ImagesDir;
        TxtLogDir.Text = AppPaths.LogsDir;
        TxtVersion.Text = $"版本 {GetVersion()}";
        await RefreshStatsAsync();
        await RefreshBackupStateAsync();
    }

    private static string GetVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version == null ? "开发版" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private async Task RefreshStatsAsync()
    {
        var imageCount = CountFiles(AppPaths.ImagesDir);
        var imageSize = GetDirectorySize(AppPaths.ImagesDir);
        var thumbnailCount = CountFiles(AppPaths.ThumbnailsDir);
        var thumbnailSize = GetDirectorySize(AppPaths.ThumbnailsDir);
        var logCount = CountFiles(AppPaths.LogsDir);
        var logSize = GetDirectorySize(AppPaths.LogsDir);
        var missingImageCount = await _imageMaintenanceService.CountMissingImagesAsync();
        var missingThumbnailCount = await _imageMaintenanceService.CountMissingThumbnailsAsync();

        TxtImageStats.Text = $"{imageCount} 张原图 · {FormatSize(imageSize)}";
        TxtCacheStats.Text = $"{thumbnailCount} 个缩略图缓存 · {FormatSize(thumbnailSize)}";
        TxtThumbnailHealthStats.Text = BuildThumbnailHealthText(missingThumbnailCount);
        TxtLogStats.Text = $"{logCount} 个日志文件 · {FormatSize(logSize)}";
        TxtMissingImageStats.Text = missingImageCount == 0
            ? "没有发现缺失图片"
            : $"{missingImageCount} 件衣服的图片路径失效";
    }

    private static int CountFiles(string directory)
    {
        if (!Directory.Exists(directory))
            return 0;
        return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Count();
    }

    private static long GetDirectorySize(string directory)
    {
        if (!Directory.Exists(directory))
            return 0;
        return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Sum(file => new FileInfo(file).Length);
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:0.#} {units[unitIndex]}";
    }

    private static void OpenPath(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
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
    }

    private async void ClearThumbnails_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定清理缩略图缓存吗？原始图片不会被删除。",
            "清理缓存",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.OK)
            return;

        if (Directory.Exists(AppPaths.ThumbnailsDir))
        {
            foreach (var file in Directory.EnumerateFiles(AppPaths.ThumbnailsDir, "*", SearchOption.AllDirectories))
                File.Delete(file);
        }

        await RefreshStatsAsync();
        MessageBox.Show("缩略图缓存已清理。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void RebuildThumbnails_Click(object sender, RoutedEventArgs e)
    {
        var result = await _imageMaintenanceService.RebuildMissingThumbnailsAsync();
        await RefreshStatsAsync();

        MessageBox.Show(
            result.Summary,
            "缩略图缓存",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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

        if (Directory.Exists(AppPaths.LogsDir))
        {
            var today = DateTime.Today;
            foreach (var file in Directory.EnumerateFiles(AppPaths.LogsDir, "*.log", SearchOption.TopDirectoryOnly))
            {
                var info = new FileInfo(file);
                if (info.LastWriteTime.Date >= today)
                    continue;

                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // The log view should remain usable even if a file is locked by another process.
                }
            }
        }

        await RefreshStatsAsync();
        MessageBox.Show("历史日志已清理。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void ExportBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "ZIP 备份包|*.zip|JSON 备份|*.json",
            DefaultExt = ".zip",
            FileName = Path.GetFileName(_backupService.BuildDefaultBackupPath()),
            InitialDirectory = AppPaths.BackupsDir
        };

        if (dialog.ShowDialog() != true)
            return;

        var validation = await _backupService.ValidateExportAsync(dialog.FileName);
        if (!ConfirmExport(validation))
            return;

        var result = await _backupService.ExportAsync(dialog.FileName);
        await RefreshBackupStateAsync();

        MessageBox.Show(
            BuildExportMessage(result),
            "完成",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void QuickExportBackup_Click(object sender, RoutedEventArgs e)
    {
        var filePath = _backupService.BuildDefaultBackupPath();
        var validation = await _backupService.ValidateExportAsync(filePath);
        if (!ConfirmExport(validation))
            return;

        var result = await _backupService.ExportAsync(filePath);
        await RefreshBackupStateAsync();

        MessageBox.Show(
            BuildExportMessage(result),
            "完成",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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

        var result = await _backupService.ImportAsync(dialog.FileName);
        await RefreshStatsAsync();
        await RefreshBackupStateAsync(result);

        MessageBox.Show(
            BuildImportMessage(result),
            "完成",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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

        MessageBox.Show(
            repairedCount == 0
                ? "没有找到可重连的图片文件。"
                : $"已修复 {repairedCount} 张缺失图片。",
            "图片修复",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void RefreshBackupState_Click(object sender, RoutedEventArgs e)
    {
        await RefreshBackupStateAsync();
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

        await _backupService.ClearHistoryAsync();
        await RefreshBackupStateAsync();
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
        var previewPath = Path.Combine(AppPaths.BackupsDir, $"preview-{Guid.NewGuid():N}.zip");
        var validation = await _backupService.ValidateExportAsync(previewPath);
        UpdateBackupValidationCard(validation);

        var history = await _backupService.GetHistoryAsync();
        BackupHistoryList.ItemsSource = history;
        TxtBackupHistoryEmpty.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (latestImport != null)
        {
            UpdateLatestImportCard(latestImport);
            return;
        }

        var latestImportHistory = history.FirstOrDefault(item => item.Operation == "Import" && item.Success);
        if (latestImportHistory == null)
        {
            ResetLatestImportCard();
            return;
        }

        TxtLastImportSummary.Text = latestImportHistory.Summary;
        TxtLastImportDetail.Text = $"{latestImportHistory.TimestampText} · {latestImportHistory.FileName}";
        LastImportWarningCard.Visibility = Visibility.Collapsed;
        LastImportMissingCard.Visibility = Visibility.Collapsed;
        BtnRepairAfterImport.Visibility = Visibility.Collapsed;
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

    private static string BuildValidationHint(BackupValidationResult validation)
    {
        if (validation.IsEmptyBackup)
            return validation.ReadinessSummary;

        if (!validation.HasWarnings)
            return "当前可以直接导出 ZIP 备份包，建议优先使用 ZIP 保留图片。";

        return string.Join(" ", validation.Warnings);
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

    private static string BuildThumbnailHealthText(int missingThumbnailCount)
    {
        return missingThumbnailCount == 0
            ? "所有已存在的原图都已经生成缩略图。"
            : $"{missingThumbnailCount} 张图片缺少缩略图，可一键重建缓存。";
    }

    // 导出前校验卡片集中在这里组装，避免散落的文本更新让状态变得难追踪。
    private void UpdateBackupValidationCard(BackupValidationResult validation)
    {
        TxtBackupValidation.Text = validation.ReadinessSummary;
        TxtBackupValidationData.Text = validation.DataSummary;
        TxtBackupValidationImages.Text = validation.ImageSummary;
        TxtBackupValidationHint.Text = BuildValidationHint(validation);

        if (validation.HasWarnings)
        {
            BackupValidationWarningCard.Visibility = Visibility.Visible;
            TxtBackupValidationWarnings.Text = string.Join("\n", validation.Warnings);
        }
        else
        {
            BackupValidationWarningCard.Visibility = Visibility.Collapsed;
            TxtBackupValidationWarnings.Text = string.Empty;
        }
    }

    private void UpdateLatestImportCard(BackupImportResult result)
    {
        TxtLastImportSummary.Text = result.Summary;
        TxtLastImportDetail.Text =
            $"{result.ImportedAt:yyyy-MM-dd HH:mm} · {Path.GetFileName(result.FilePath)}\n" +
            $"衣服 {result.ClothingCount} · 搭配 {result.OutfitCount} · 标签 {result.TagCount} · 恢复图片 {result.RestoredImageCount}";

        if (result.Warnings.Count > 0)
        {
            LastImportWarningCard.Visibility = Visibility.Visible;
            TxtLastImportWarning.Text = string.Join(" ", result.Warnings);
            BtnRepairAfterImport.Visibility = result.ShouldSuggestRepair ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            LastImportWarningCard.Visibility = Visibility.Collapsed;
            BtnRepairAfterImport.Visibility = Visibility.Collapsed;
        }

        if (result.MissingImageFiles.Count > 0)
        {
            LastImportMissingCard.Visibility = Visibility.Visible;
            TxtLastImportMissingFiles.Text = string.Join("、", result.MissingImageFiles.Take(6)) +
                (result.MissingImageFiles.Count > 6 ? $" 等 {result.MissingImageFiles.Count} 个文件" : string.Empty);
        }
        else
        {
            LastImportMissingCard.Visibility = Visibility.Collapsed;
            TxtLastImportMissingFiles.Text = string.Empty;
        }
    }

    private void ResetLatestImportCard()
    {
        TxtLastImportSummary.Text = "还没有导入记录。";
        TxtLastImportDetail.Text = "导入完成后，这里会显示恢复结果和后续建议。";
        LastImportWarningCard.Visibility = Visibility.Collapsed;
        LastImportMissingCard.Visibility = Visibility.Collapsed;
        BtnRepairAfterImport.Visibility = Visibility.Collapsed;
        TxtLastImportWarning.Text = string.Empty;
        TxtLastImportMissingFiles.Text = string.Empty;
    }
}
