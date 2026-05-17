using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClosetApp.UI.Services;

public static class ClothingImageLoader
{
    private const int ThumbnailPreferredMaxWidth = 260;

    private static readonly string ImageFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClosetApp", "images");
    private static readonly string ThumbnailFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClosetApp", "thumbnails");

    private static readonly ConcurrentDictionary<string, ImageSource?> ImageCache = new();
    private static readonly ConcurrentDictionary<string, Size?> SizeCache = new();

    public static ImageSource? Load(string? path, int decodePixelWidth = 400)
    {
        var resolved = ResolvePath(path, preferThumbnail: decodePixelWidth <= ThumbnailPreferredMaxWidth);
        if (resolved == null)
            return null;

        var key = BuildCacheKey(resolved, decodePixelWidth);
        if (ImageCache.TryGetValue(key, out var cached))
            return cached;

        var image = LoadCore(resolved, decodePixelWidth);
        if (image != null)
            ImageCache.TryAdd(key, image);

        return image;
    }

    public static Size? GetDisplaySize(string? path, int decodePixelWidth = 400)
    {
        var resolved = ResolvePath(path, preferThumbnail: true);
        if (resolved == null)
            return null;

        var key = BuildCacheKey(resolved, decodePixelWidth);
        return SizeCache.GetOrAdd(key, _ =>
        {
            var image = Load(resolved, decodePixelWidth);
            return image == null ? null : new Size(image.Width, image.Height);
        });
    }

    public static string? ResolvePath(string? path, bool preferThumbnail = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (File.Exists(path))
            return path;

        var appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        if (File.Exists(appPath))
            return appPath;

        var thumbnailPath = BuildThumbnailPath(path);
        if (preferThumbnail && File.Exists(thumbnailPath))
            return thumbnailPath;

        var localPath = Path.Combine(ImageFolder, path);
        if (File.Exists(localPath))
            return localPath;

        return File.Exists(thumbnailPath) ? thumbnailPath : null;
    }

    private static ImageSource? LoadCore(string path, int decodePixelWidth)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = decodePixelWidth;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildCacheKey(string path, int decodePixelWidth)
    {
        var ticks = File.GetLastWriteTimeUtc(path).Ticks;
        return $"{path}|{ticks}|{decodePixelWidth}";
    }

    private static string BuildThumbnailPath(string relativePath)
    {
        var name = Path.GetFileNameWithoutExtension(relativePath);
        var ext = Path.GetExtension(relativePath);
        return Path.Combine(ThumbnailFolder, $"{name}_thumb{ext}");
    }
}
