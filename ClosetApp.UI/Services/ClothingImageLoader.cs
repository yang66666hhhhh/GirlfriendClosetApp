using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClosetApp.Application.Images;

namespace ClosetApp.UI.Services;

public static class ClothingImageLoader
{
    private const byte TransparentBackgroundThreshold = 12;
    private const byte LightBackgroundThreshold = 244;
    private const byte NeutralBackgroundThreshold = 228;
    private const byte NeutralBackgroundTolerance = 16;
    private const int TrimSafetyPadding = 4;

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

    public static ImageSource? Load(
        string? path,
        ImageVariant variant,
        int decodePixelWidth = 400,
        bool trimLightPadding = false)
    {
        var resolved = ResolvePath(path, variant);
        if (resolved == null)
            return null;

        var key = BuildCacheKey(resolved, decodePixelWidth, trimLightPadding);
        if (ImageCache.TryGetValue(key, out var cached))
            return cached;

        var image = LoadCore(resolved, decodePixelWidth, trimLightPadding);
        if (image != null)
            ImageCache.TryAdd(key, image);

        return image;
    }

    public static Size? GetDisplaySize(string? path, int decodePixelWidth = 400, bool trimLightPadding = false)
    {
        var resolved = ResolvePath(path, ImageVariant.Display);
        if (resolved == null)
            return null;

        var key = BuildCacheKey(resolved, decodePixelWidth, trimLightPadding);
        return SizeCache.GetOrAdd(key, _ =>
        {
            var image = LoadCore(resolved, decodePixelWidth, trimLightPadding);
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

    private static ImageSource? LoadCore(string path, int decodePixelWidth, bool trimLightPadding)
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

            if (!trimLightPadding)
                return bitmap;

            return TryTrimLightPadding(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildCacheKey(string path, int decodePixelWidth, bool trimLightPadding)
    {
        var ticks = File.GetLastWriteTimeUtc(path).Ticks;
        return $"{path}|{ticks}|{decodePixelWidth}|trim:{trimLightPadding}";
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

    private static ImageSource TryTrimLightPadding(BitmapSource source)
    {
        try
        {
            var normalized = source.Format == PixelFormats.Bgra32
                ? source
                : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            if (normalized.CanFreeze && !normalized.IsFrozen)
                normalized.Freeze();

            var bounds = FindLightPaddingBounds(normalized);
            if (bounds == null)
                return source;

            var rect = bounds.Value;
            if (rect.X == 0 &&
                rect.Y == 0 &&
                rect.Width == normalized.PixelWidth &&
                rect.Height == normalized.PixelHeight)
            {
                return source;
            }

            var cropped = new CroppedBitmap(normalized, rect);
            cropped.Freeze();
            return cropped;
        }
        catch
        {
            return source;
        }
    }

    private static Int32Rect? FindLightPaddingBounds(BitmapSource source)
    {
        if (source.PixelWidth <= 0 || source.PixelHeight <= 0)
            return null;

        var stride = ((source.PixelWidth * source.Format.BitsPerPixel) + 7) / 8;
        var pixels = new byte[stride * source.PixelHeight];
        source.CopyPixels(pixels, stride, 0);

        var left = 0;
        var right = source.PixelWidth - 1;
        var top = 0;
        var bottom = source.PixelHeight - 1;

        while (top < bottom && IsRemovableRow(pixels, stride, source.PixelWidth, top))
            top++;

        while (bottom > top && IsRemovableRow(pixels, stride, source.PixelWidth, bottom))
            bottom--;

        while (left < right && IsRemovableColumn(pixels, stride, source.PixelWidth, source.PixelHeight, left, top, bottom))
            left++;

        while (right > left && IsRemovableColumn(pixels, stride, source.PixelWidth, source.PixelHeight, right, top, bottom))
            right--;

        left = Math.Max(0, left - TrimSafetyPadding);
        top = Math.Max(0, top - TrimSafetyPadding);
        right = Math.Min(source.PixelWidth - 1, right + TrimSafetyPadding);
        bottom = Math.Min(source.PixelHeight - 1, bottom + TrimSafetyPadding);

        var width = right - left + 1;
        var height = bottom - top + 1;
        if (width <= 0 || height <= 0)
            return null;

        return new Int32Rect(left, top, width, height);
    }

    private static bool IsRemovableRow(byte[] pixels, int stride, int width, int row)
    {
        var allowedContentPixels = Math.Max(2, width / 120);
        var contentCount = 0;
        var rowOffset = row * stride;

        for (var x = 0; x < width; x++)
        {
            if (IsBackgroundPixel(pixels, rowOffset + (x * 4)))
                continue;

            contentCount++;
            if (contentCount > allowedContentPixels)
                return false;
        }

        return true;
    }

    private static bool IsRemovableColumn(byte[] pixels, int stride, int width, int height, int column, int top, int bottom)
    {
        var spanHeight = Math.Max(1, bottom - top + 1);
        var allowedContentPixels = Math.Max(2, spanHeight / 120);
        var contentCount = 0;

        for (var y = top; y <= bottom && y < height; y++)
        {
            if (IsBackgroundPixel(pixels, (y * stride) + (column * 4)))
                continue;

            contentCount++;
            if (contentCount > allowedContentPixels)
                return false;
        }

        return true;
    }

    private static bool IsBackgroundPixel(byte[] pixels, int offset)
    {
        var blue = pixels[offset];
        var green = pixels[offset + 1];
        var red = pixels[offset + 2];
        var alpha = pixels[offset + 3];
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var neutralLight = red >= NeutralBackgroundThreshold &&
                           green >= NeutralBackgroundThreshold &&
                           blue >= NeutralBackgroundThreshold &&
                           max - min <= NeutralBackgroundTolerance;

        return alpha <= TransparentBackgroundThreshold ||
               (red >= LightBackgroundThreshold &&
                green >= LightBackgroundThreshold &&
                blue >= LightBackgroundThreshold) ||
               neutralLight;
    }
}
