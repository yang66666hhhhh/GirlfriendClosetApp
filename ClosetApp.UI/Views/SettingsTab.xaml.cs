using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Infrastructure;

namespace ClosetApp.UI.Views;

public partial class SettingsTab : UserControl
{
    public SettingsTab()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadSettings();
    }

    private void LoadSettings()
    {
        TxtDataDir.Text = AppPaths.BaseDir;
        TxtImagesDir.Text = AppPaths.ImagesDir;
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

        TxtImageStats.Text = $"{imageCount} 张原图 · {FormatSize(imageSize)}";
        TxtCacheStats.Text = $"{thumbnailCount} 个缩略图缓存 · {FormatSize(thumbnailSize)}";
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
}
