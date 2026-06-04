using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media.Imaging;

namespace ClosetApp.UI.Services;

public static class AiImageBitmapCache
{
    private static readonly ConcurrentDictionary<string, BitmapImage?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static BitmapImage? GetOrLoad(string absolutePath, int decodePixelWidth)
    {
        if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            return null;

        var key = $"{absolutePath}|{decodePixelWidth}";
        return Cache.GetOrAdd(key, _ => LoadBitmap(absolutePath, decodePixelWidth));
    }

    public static void Invalidate(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
            return;

        var prefix = $"{absolutePath}|";
        foreach (var key in Cache.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            Cache.TryRemove(key, out _);
    }

    private static BitmapImage? LoadBitmap(string absolutePath, int decodePixelWidth)
    {
        if (!File.Exists(absolutePath))
            return null;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(absolutePath);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = decodePixelWidth;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
