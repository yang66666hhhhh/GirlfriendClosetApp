using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Settings;

public partial class StorageLocationsSettingsPanel : UserControl
{
    private IImageStorageService? _imageStorageService;

    public StorageLocationsSettingsPanel()
    {
        InitializeComponent();
        TxtDataDir.Text = AppPaths.BaseDir;
    }

    public Task RefreshAsync()
    {
        _imageStorageService ??= App.Services.GetService<IImageStorageService>();
        if (_imageStorageService != null)
        {
            TxtImagesDir.Text = _imageStorageService.GetOriginalsDirectory();
        }
        else
        {
            TxtImagesDir.Text = AppPaths.BaseDir;
        }
        return Task.CompletedTask;
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

    private string ResolveImagesRoot()
    {
        _imageStorageService ??= App.Services.GetService<IImageStorageService>();
        if (_imageStorageService != null)
        {
            // 打开图片根目录（原图目录的父级），便于用户同时看到 originals / display / thumbnails。
            var originalsDir = _imageStorageService.GetOriginalsDirectory();
            var parent = Path.GetDirectoryName(originalsDir);
            if (!string.IsNullOrWhiteSpace(parent))
                return parent;
            return originalsDir;
        }
        return AppPaths.BaseDir;
    }

    private string ResolveThumbnailsDir()
    {
        _imageStorageService ??= App.Services.GetService<IImageStorageService>();
        return _imageStorageService?.GetThumbnailsDirectory() ?? AppPaths.BaseDir;
    }

    private void OpenDataDir_Click(object sender, RoutedEventArgs e) => OpenPath(AppPaths.BaseDir);
    private void OpenDatabase_Click(object sender, RoutedEventArgs e) => RevealFile(AppPaths.DatabasePath);
    private void OpenImagesDir_Click(object sender, RoutedEventArgs e) => OpenPath(ResolveImagesRoot());
    private void OpenThumbnailsDir_Click(object sender, RoutedEventArgs e) => OpenPath(ResolveThumbnailsDir());
    private void OpenAppDir_Click(object sender, RoutedEventArgs e) => OpenPath(AppDomain.CurrentDomain.BaseDirectory);
}
