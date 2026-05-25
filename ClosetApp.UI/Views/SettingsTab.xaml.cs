using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Views;

public partial class SettingsTab : UserControl
{
    private readonly IBackupService _backupService;
    private readonly IImageMaintenanceService _imageMaintenanceService;
    private readonly IWeatherService _weatherService;
    private readonly IWeatherPreferencesService _weatherPreferencesService;
    private readonly ThemeService _themeService;
    private bool _isRefreshingWeather;

    public SettingsTab()
    {
        _backupService = App.Services.GetRequiredService<IBackupService>();
        _imageMaintenanceService = App.Services.GetRequiredService<IImageMaintenanceService>();
        _weatherService = App.Services.GetRequiredService<IWeatherService>();
        _weatherPreferencesService = App.Services.GetRequiredService<IWeatherPreferencesService>();
        _themeService = App.Services.GetRequiredService<ThemeService>();
        InitializeComponent();
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
            await LoadWeatherPreferencesAsync();
            ApplyThemeSelectionState(_themeService.CurrentTheme);
            await RefreshStatsAsync();
            await RefreshBackupStateAsync();
            await RefreshWeatherAsync(showStatus: false);
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
        var originalCount = CountFiles(AppPaths.OriginalsDir);
        var originalSize = GetDirectorySize(AppPaths.OriginalsDir);
        var displayCount = CountFiles(AppPaths.DisplayDir);
        var displaySize = GetDirectorySize(AppPaths.DisplayDir);
        var thumbnailCount = CountFiles(AppPaths.ThumbnailsDir);
        var thumbnailSize = GetDirectorySize(AppPaths.ThumbnailsDir);
        var logCount = CountFiles(AppPaths.LogsDir);
        var logSize = GetDirectorySize(AppPaths.LogsDir);
        var missingImageCount = await _imageMaintenanceService.CountMissingImagesAsync();
        var missingThumbnailCount = await _imageMaintenanceService.CountMissingThumbnailsAsync();
        var orphanOriginals = await _imageMaintenanceService.AnalyzeOrphanOriginalsAsync();

        TxtImageStats.Text = $"{originalCount} 张原图 · {FormatSize(originalSize)}";
        TxtCacheStats.Text = $"{displayCount} 个主视觉缓存 · {thumbnailCount} 个小预览缓存 · {FormatSize(displaySize + thumbnailSize)}";
        TxtThumbnailHealthStats.Text = BuildThumbnailHealthText(missingThumbnailCount);
        TxtOrphanOriginalStats.Text = BuildOrphanOriginalsText(orphanOriginals);
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
        ToastService.Instance.ShowInfo("统计信息已刷新。");
    }

    private async void RefreshWeather_Click(object sender, RoutedEventArgs e)
    {
        await RefreshWeatherAsync(showStatus: true);
    }

    private async void SaveWeatherCity_Click(object sender, RoutedEventArgs e)
    {
        var city = TxtWeatherCity.Text.Trim();
        if (string.IsNullOrWhiteSpace(city))
        {
            ShowWeatherStatus("请先输入默认城市。");
            TxtWeatherCity.Focus();
            return;
        }

        await _weatherPreferencesService.SaveAsync(new WeatherPreferences
        {
            DefaultCity = city
        });

        ShowWeatherStatus($"默认城市已保存为 {city}。");
        ToastService.Instance.ShowSuccess("已保存默认城市", city);
        await RequestAppRefreshAsync(outfits: true);
    }

    private async void UseRoseTheme_Click(object sender, RoutedEventArgs e)
    {
        await _themeService.ApplyThemeAsync(AppThemeKind.Rose);
        ApplyThemeSelectionState(AppThemeKind.Rose);
        ToastService.Instance.ShowSuccess("已切换到柔粉主题");
    }

    private async void UseBlueTheme_Click(object sender, RoutedEventArgs e)
    {
        await _themeService.ApplyThemeAsync(AppThemeKind.Blue);
        ApplyThemeSelectionState(AppThemeKind.Blue);
        ToastService.Instance.ShowSuccess("已切换到清蓝主题");
    }

    private async void ClearThumbnails_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定清理图片缓存吗？原始图片不会被删除。",
            "清理缓存",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.OK)
            return;

        DeleteFilesInDirectory(AppPaths.DisplayDir);
        DeleteFilesInDirectory(AppPaths.ThumbnailsDir);

        await RefreshStatsAsync();
        MessageBox.Show("图片缓存已清理。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess("图片缓存已清理");
    }

    private async void RebuildThumbnails_Click(object sender, RoutedEventArgs e)
    {
        var result = await _imageMaintenanceService.RebuildMissingThumbnailsAsync();
        await RefreshStatsAsync();

        MessageBox.Show(
            result.Summary,
            "图片缓存",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
            $"发现 {analysis.OrphanCount} 张数据库未引用的原图，占用 {FormatSize(analysis.TotalBytes)}。\n\n清理会同时删除这些原图对应的主视觉和小预览缓存，但不会删除任何仍被衣物引用的图片。确定继续吗？",
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
        ToastService.Instance.ShowSuccess("历史日志已清理");
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
        ToastService.Instance.ShowSuccess("备份已导出", Path.GetFileName(result.FilePath));
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
        ToastService.Instance.ShowSuccess("备份已导出", Path.GetFileName(result.FilePath));
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
        await RequestAppRefreshAsync(clothes: true, outfits: true, tags: true);

        MessageBox.Show(
            BuildImportMessage(result),
            "完成",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess("备份已导入", "衣柜、搭配和标签列表已经刷新。");
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

    private async Task LoadWeatherPreferencesAsync()
    {
        var preferences = await _weatherPreferencesService.GetAsync();
        TxtWeatherCity.Text = preferences.DefaultCity;
    }

    private async Task RefreshWeatherAsync(bool showStatus)
    {
        if (_isRefreshingWeather)
            return;

        var city = TxtWeatherCity.Text.Trim();
        if (string.IsNullOrWhiteSpace(city))
        {
            UpdateWeatherCard(null, "请输入城市后再刷新。");
            return;
        }

        _isRefreshingWeather = true;
        BtnRefreshWeather.IsEnabled = false;
        BtnSaveWeatherCity.IsEnabled = false;
        TxtWeatherSummary.Text = "正在获取天气...";
        TxtWeatherDetails.Text = "稍等一下，我正在请求实时天气。";
        TxtWeatherObservedAt.Text = string.Empty;
        if (showStatus)
            ShowWeatherStatus("正在刷新天气...");

        try
        {
            await _weatherPreferencesService.SaveAsync(new WeatherPreferences
            {
                DefaultCity = city
            });

            var weather = await _weatherService.GetCurrentWeatherAsync(city);
            if (weather == null)
            {
                UpdateWeatherCard(null, $"没有找到“{city}”的天气数据，请试试中文全名、英文城市名，或带上省/州名。");
                return;
            }

            UpdateWeatherCard(weather, showStatus ? $"已刷新 {weather.City} 的实时天气。" : null);
        }
        catch (Exception ex)
        {
            UpdateWeatherCard(null, $"天气刷新失败：{ex.Message}");
        }
        finally
        {
            _isRefreshingWeather = false;
            BtnRefreshWeather.IsEnabled = true;
            BtnSaveWeatherCity.IsEnabled = true;
        }
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
        ToastService.Instance.ShowSuccess("备份历史已清空");
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
            ? "所有已存在的原图都已经生成主视觉和小预览缓存。"
            : $"{missingThumbnailCount} 张图片缺少主视觉或小预览缓存，可一键重建。";
    }

    private static string BuildOrphanOriginalsText(OrphanOriginalsResult result)
    {
        return result.HasOrphans
            ? $"{result.OrphanCount} 张原图未被数据库引用，占用 {FormatSize(result.TotalBytes)}。"
            : "没有发现孤儿原图。";
    }

    private static void DeleteFilesInDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            File.Delete(file);
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

    private void UpdateWeatherCard(WeatherInfo? weather, string? status)
    {
        if (weather == null)
        {
            TxtWeatherSummary.Text = "暂时没有可用天气。";
            TxtWeatherDetails.Text = "你可以检查网络，或者换一个更完整的城市名重新试一次。";
            TxtWeatherObservedAt.Text = string.Empty;
            ShowWeatherStatus(status);
            return;
        }

        TxtWeatherSummary.Text = $"{weather.City} · {weather.Temperature}°C · {weather.Condition}";
        TxtWeatherDetails.Text = $"湿度 {weather.Humidity}%{BuildTimezoneSuffix(weather.Timezone)}";
        TxtWeatherObservedAt.Text = weather.ObservedAt.HasValue
            ? $"观测时间 {weather.ObservedAt:yyyy-MM-dd HH:mm}"
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(status))
            ShowWeatherStatus(status);
        else
            HideWeatherStatus();
    }

    private static string BuildTimezoneSuffix(string timezone)
    {
        return string.IsNullOrWhiteSpace(timezone) ? string.Empty : $" · {timezone}";
    }

    private void ShowWeatherStatus(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            HideWeatherStatus();
            return;
        }

        WeatherStatusCard.Visibility = Visibility.Visible;
        TxtWeatherStatus.Text = message;
    }

    private void HideWeatherStatus()
    {
        WeatherStatusCard.Visibility = Visibility.Collapsed;
        TxtWeatherStatus.Text = string.Empty;
    }

    private void ApplyThemeSelectionState(AppThemeKind theme)
    {
        bool isRose = theme == AppThemeKind.Rose;

        ApplyThemeCardState(
            ThemeRoseCard,
            ThemeRoseCurrentBadge,
            BtnUseRoseTheme,
            isRose,
            Color.FromRgb(249, 245, 241),  // Rose surface
            Color.FromRgb(230, 213, 216),  // Rose border
            "使用柔粉");

        ApplyThemeCardState(
            ThemeBlueCard,
            ThemeBlueCurrentBadge,
            BtnUseBlueTheme,
            !isRose,
            Color.FromRgb(235, 241, 251),  // Blue surface
            Color.FromRgb(200, 214, 237),  // Blue border
            "使用清蓝");

        ApplyThemeSwatchState(
            isRose,
            ThemeRoseSwatchPrimary,
            ThemeRoseSwatchSoft,
            ThemeRoseSwatchSurface,
            Color.FromRgb(202, 156, 159),
            Color.FromRgb(247, 240, 238),
            Color.FromRgb(249, 245, 241));

        ApplyThemeSwatchState(
            !isRose,
            ThemeBlueSwatchPrimary,
            ThemeBlueSwatchSoft,
            ThemeBlueSwatchSurface,
            Color.FromRgb(88, 129, 214),
            Color.FromRgb(224, 236, 255),
            Color.FromRgb(235, 241, 251));

        // Use unified theme colors for appearance section
        var primary = GetBrushColor("PrimaryBrush");
        var primaryLight = GetBrushColor("PrimaryLightBrush");
        var primaryText = GetBrushColor("PrimaryDarkBrush");

        ApplyAppearanceSectionState(primary, primaryLight, primaryText);

        TxtThemeSummary.Text = isRose ? "当前使用柔粉主题" : "当前使用清蓝主题";
        TxtThemeDescription.Text = isRose
            ? "柔粉更柔和、沉稳，能保留生活感，也不会抢照片和衣物本身的视觉重点。"
            : "清蓝更克制、清爽，页面会更冷静，也更偏中性工具感。";
    }

    private void ApplyThemeCardState(
        Border card,
        Border badge,
        Button button,
        bool isSelected,
        Color previewSurface,
        Color previewBorder,
        string idleText)
    {
        var primary = GetBrushColor("PrimaryBrush");
        var primaryLight = GetBrushColor("PrimaryLightBrush");
        var borderLight = GetBrushColor("BorderLightBrush");
        var textSecondary = GetBrushColor("TextSecondaryBrush");

        card.BorderThickness = new Thickness(isSelected ? 2.5 : 1.25);
        card.BorderBrush = new SolidColorBrush(isSelected ? primary : previewBorder);
        card.Background = new SolidColorBrush(previewSurface);
        card.Opacity = 1;
        card.Effect = isSelected
            ? new DropShadowEffect
            {
                Color = primary,
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.18
            }
            : null;

        badge.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
        badge.Background = new SolidColorBrush(primaryLight);
        badge.BorderBrush = new SolidColorBrush(primary);

        button.Content = isSelected ? "已启用" : idleText;
        button.IsEnabled = !isSelected;
        button.Background = new SolidColorBrush(isSelected ? previewSurface : primaryLight);
        button.BorderBrush = new SolidColorBrush(isSelected ? borderLight : primary);
        button.Foreground = new SolidColorBrush(isSelected ? textSecondary : primary);
    }

    private void ApplyThemeSwatchState(
        bool isSelected,
        Border primarySwatch,
        Border softSwatch,
        Border surfaceSwatch,
        Color primaryColor,
        Color softColor,
        Color surfaceColor)
    {
        primarySwatch.Background = new SolidColorBrush(primaryColor);
        softSwatch.Background = new SolidColorBrush(softColor);
        surfaceSwatch.Background = new SolidColorBrush(surfaceColor);
        primarySwatch.Opacity = 1;
        softSwatch.Opacity = 1;
        surfaceSwatch.Opacity = 1;
    }

    private void ApplyAppearanceSectionState(
        Color primary,
        Color primaryLight,
        Color primaryText)
    {
        var surfaceElevated = GetBrushColor("SurfaceElevatedBrush");

        AppearanceSectionCard.Background = new SolidColorBrush(surfaceElevated);
        AppearanceSectionCard.BorderBrush = new SolidColorBrush(primary);

        AppearanceSectionBadge.Background = new SolidColorBrush(primaryLight);
        AppearanceSectionBadge.BorderBrush = new SolidColorBrush(primary);
        AppearanceSectionBadgeText.Foreground = new SolidColorBrush(primaryText);

        ThemeSelectionPanel.Background = new SolidColorBrush(surfaceElevated);
        ThemeSelectionPanel.BorderBrush = new SolidColorBrush(primary);
        ThemeSelectionPanel.BorderThickness = new Thickness(1);

        AppearanceAppInfoCard.Background = new SolidColorBrush(surfaceElevated);
        AppearanceAppInfoCard.BorderBrush = new SolidColorBrush(primary);
        AppearanceAppInfoCard.BorderThickness = new Thickness(1);

        BtnAppearanceOpenAppDir.Background = new SolidColorBrush(primaryLight);
        BtnAppearanceOpenAppDir.BorderBrush = new SolidColorBrush(primary);
        BtnAppearanceOpenAppDir.Foreground = new SolidColorBrush(primary);
    }

    private Color GetBrushColor(string key)
    {
        return FindResource(key) is SolidColorBrush brush
            ? brush.Color
            : Colors.Transparent;
    }

    private static Color BlendWithWhite(Color color, double amount)
    {
        byte Blend(byte channel) => (byte)(channel + (255 - channel) * amount);

        return Color.FromArgb(
            color.A,
            Blend(color.R),
            Blend(color.G),
            Blend(color.B));
    }
}
