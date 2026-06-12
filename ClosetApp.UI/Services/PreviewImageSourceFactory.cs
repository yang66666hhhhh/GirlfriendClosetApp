using System.IO;
using System.Collections.Concurrent;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace ClosetApp.UI.Services;

public static class PreviewImageSourceFactory
{
    private static readonly ConcurrentDictionary<string, BitmapSource> BitmapCache = new();

    public static BitmapSource? TryCreateBitmapSource(string? path, int decodePixelWidth = 0)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        var cacheKey = BuildCacheKey(path, decodePixelWidth);
        if (BitmapCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var bytes = TryCreateNormalizedPngBytes(path);
        if (bytes == null)
            return null;

        using var stream = new MemoryStream(bytes);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        if (decodePixelWidth > 0)
            bitmap.DecodePixelWidth = decodePixelWidth;
        bitmap.EndInit();
        bitmap.Freeze();
        BitmapCache[cacheKey] = bitmap;
        return bitmap;
    }

    // 统一把任意可识别图片转成内存 PNG，避免 WPF 预览依赖文件后缀或本机解码器。
    public static byte[]? TryCreateNormalizedPngBytes(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            using var image = Image.Load<Rgba32>(path);
            using var stream = new MemoryStream();
            image.Save(stream, new PngEncoder());
            return stream.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static string BuildCacheKey(string path, int decodePixelWidth)
    {
        var fileInfo = new FileInfo(path);
        return $"{Path.GetFullPath(path)}|{decodePixelWidth}|{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks}";
    }
}
