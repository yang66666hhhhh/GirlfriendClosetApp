using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClosetApp.Application.Images;

namespace ClosetApp.UI.Services;

public static class ClothingImageLoader
{
    private static readonly string ImagesFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClosetApp", "images");
    private static readonly string OriginalFolder = Path.Combine(ImagesFolder, "originals");
    private static readonly string DisplayFolder = Path.Combine(ImagesFolder, "display");
    private static readonly string ThumbnailFolder = Path.Combine(ImagesFolder, "thumbnails");

    private static readonly ConcurrentDictionary<string, ImageSource?> ImageCache = new();
    private static readonly ConcurrentDictionary<string, Size?> SizeCache = new();

    public static ImageSource? Load(string? path, int decodePixelWidth = 400)
    {
        return Load(path, ImageVariant.Display, decodePixelWidth);
    }

    public static ImageSource? Load(string? path, ImageVariant variant, int decodePixelWidth = 400)
    {
        var resolved = ResolvePath(path, variant);
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
        var resolved = ResolvePath(path, ImageVariant.Display);
        if (resolved == null)
            return null;

        var key = BuildCacheKey(resolved, decodePixelWidth);
        return SizeCache.GetOrAdd(key, _ =>
        {
            var image = LoadCore(resolved, decodePixelWidth);
            return image == null ? null : new Size(image.Width, image.Height);
        });
    }

    public static string? ResolvePath(string? path, bool preferThumbnail = false)
    {
        return ResolvePath(path, preferThumbnail ? ImageVariant.Thumbnail : ImageVariant.Display);
    }

    public static string? ResolvePath(string? path, ImageVariant variant)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (File.Exists(path))
            return path;

        var appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        if (File.Exists(appPath))
            return appPath;

        var originalPath = Path.Combine(OriginalFolder, path);
        var displayPath = Path.Combine(DisplayFolder, path);
        var thumbnailPath = BuildThumbnailPath(ThumbnailFolder, path);

        return variant switch
        {
            ImageVariant.Original => FirstExisting(originalPath, displayPath, thumbnailPath),
            ImageVariant.Thumbnail => FirstExisting(thumbnailPath, displayPath, originalPath),
            _ => FirstExisting(displayPath, originalPath, thumbnailPath)
        };
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

    private static string BuildThumbnailPath(string thumbnailFolder, string relativePath)
    {
        var name = Path.GetFileNameWithoutExtension(relativePath);
        var ext = Path.GetExtension(relativePath);
        return Path.Combine(thumbnailFolder, $"{name}_thumb{ext}");
    }

    private static string? FirstExisting(params string[] paths)
    {
        return paths.FirstOrDefault(File.Exists);
    }
}
