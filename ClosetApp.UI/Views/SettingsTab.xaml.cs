using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Views;

public partial class SettingsTab : UserControl
{
    private readonly IBackupService _backupService;

    public SettingsTab()
    {
        _backupService = App.Services.GetRequiredService<IBackupService>();
        InitializeComponent();
        Loaded += (_, _) => LoadSettings();
    }

    private void LoadSettings()
    {
        TxtDataDir.Text = AppPaths.BaseDir;
        TxtImagesDir.Text = AppPaths.ImagesDir;
        TxtLogDir.Text = AppPaths.LogsDir;
        TxtVersion.Text = $"版本 {GetVersion()}";
        RefreshStats();
    }

    private static string GetVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version == null ? "开发版" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private void RefreshStats()
    {
        var imageCount = CountFiles(AppPaths.ImagesDir);
        var imageSize = GetDirectorySize(AppPaths.ImagesDir);
        var thumbnailCount = CountFiles(AppPaths.ThumbnailsDir);
        var thumbnailSize = GetDirectorySize(AppPaths.ThumbnailsDir);
        var logCount = CountFiles(AppPaths.LogsDir);
        var logSize = GetDirectorySize(AppPaths.LogsDir);

        TxtImageStats.Text = $"{imageCount} 张原图 · {FormatSize(imageSize)}";
        TxtCacheStats.Text = $"{thumbnailCount} 个缩略图缓存 · {FormatSize(thumbnailSize)}";
        TxtLogStats.Text = $"{logCount} 个日志文件 · {FormatSize(logSize)}";
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

    private void OpenAppDir_Click(object sender, RoutedEventArgs e) => OpenPath(AppDomain.CurrentDomain.BaseDirectory);

    private void RefreshStats_Click(object sender, RoutedEventArgs e) => RefreshStats();

    private void ClearThumbnails_Click(object sender, RoutedEventArgs e)
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

        RefreshStats();
        MessageBox.Show("缩略图缓存已清理。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ClearLogs_Click(object sender, RoutedEventArgs e)
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

        RefreshStats();
        MessageBox.Show("历史日志已清理。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void ExportBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON 文件|*.json",
            DefaultExt = ".json",
            FileName = $"closet-backup-{DateTime.Now:yyyyMMdd-HHmm}.json",
            InitialDirectory = AppPaths.BaseDir
        };

        if (dialog.ShowDialog() != true)
            return;

        await _backupService.ExportAsync(dialog.FileName);
        MessageBox.Show("数据备份已导出。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void ImportBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON 文件|*.json",
            CheckFileExists = true,
            InitialDirectory = AppPaths.BaseDir
        };

        if (dialog.ShowDialog() != true)
            return;

        var confirm = MessageBox.Show(
            "导入会覆盖当前数据库中的衣服、搭配、标签和穿着记录。图片文件不会自动导入，确定继续吗？",
            "确认导入备份",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.OK)
            return;

        await _backupService.ImportAsync(dialog.FileName);
        RefreshStats();
        MessageBox.Show("数据备份已导入，建议返回各页面确认内容。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
