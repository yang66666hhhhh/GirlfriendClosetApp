using System;
using System.IO;

namespace ClosetApp.Infrastructure;

public static class AppPaths
{
    private static readonly string _baseDir;

    static AppPaths()
    {
        _baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClosetApp");

        EnsureDirectories();
    }

    public static string BaseDir => _baseDir;

    public static string ImagesDir => Path.Combine(_baseDir, "images");

    public static string OriginalsDir => Path.Combine(ImagesDir, "originals");

    public static string DisplayDir => Path.Combine(ImagesDir, "display");

    public static string ThumbnailsDir => Path.Combine(ImagesDir, "thumbnails");

    public static string DatabasePath => Path.Combine(_baseDir, "closet.db");

    public static string LogsDir => Path.Combine(_baseDir, "logs");

    public static string BackupsDir => Path.Combine(_baseDir, "backups");

    private static void EnsureDirectories()
    {
        Directory.CreateDirectory(_baseDir);
        Directory.CreateDirectory(ImagesDir);
        Directory.CreateDirectory(OriginalsDir);
        Directory.CreateDirectory(DisplayDir);
        Directory.CreateDirectory(ThumbnailsDir);
        Directory.CreateDirectory(LogsDir);
        Directory.CreateDirectory(BackupsDir);
    }

    public static string GetImageFullPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return string.Empty;
        return Path.Combine(OriginalsDir, relativePath);
    }

    public static string GetDisplayFullPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return string.Empty;
        return Path.Combine(DisplayDir, relativePath);
    }

    public static string GetThumbnailFullPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return string.Empty;
        var name = Path.GetFileNameWithoutExtension(relativePath);
        var ext = Path.GetExtension(relativePath);
        return Path.Combine(ThumbnailsDir, $"{name}_thumb{ext}");
    }

}
