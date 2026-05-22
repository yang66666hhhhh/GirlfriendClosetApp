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
    private const byte LightBackgroundThreshold = 232;
    private const byte NeutralBackgroundThreshold = 198;
    private const byte NeutralBackgroundTolerance = 34;
    private const byte EdgeSampleBrightnessThreshold = 178;
    private const int TrimSafetyPadding = 6;
    private const int TrimMinimumComponentArea = 160;
    private const double TrimMergeAreaRatio = 0.12;
    private const int TrimMergeGap = 36;
    private const int TrimEdgeNoiseInset = 18;
    private const int TrimEdgeNoiseMaxArea = 2400;
    private const int BackgroundSeedTolerance = 32;
    private const int BackgroundSeedLuminanceTolerance = 40;

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
        var backgroundSeeds = BuildBackgroundSeeds(pixels, stride, source.PixelWidth, source.PixelHeight);

        var subjectBounds = FindLargestSubjectBounds(pixels, stride, source.PixelWidth, source.PixelHeight, backgroundSeeds);
        if (subjectBounds != null)
            return subjectBounds;

        var left = 0;
        var right = source.PixelWidth - 1;
        var top = 0;
        var bottom = source.PixelHeight - 1;

        while (top < bottom && IsRemovableRow(pixels, stride, source.PixelWidth, top, backgroundSeeds))
            top++;

        while (bottom > top && IsRemovableRow(pixels, stride, source.PixelWidth, bottom, backgroundSeeds))
            bottom--;

        while (left < right && IsRemovableColumn(pixels, stride, source.PixelWidth, source.PixelHeight, left, top, bottom, backgroundSeeds))
            left++;

        while (right > left && IsRemovableColumn(pixels, stride, source.PixelWidth, source.PixelHeight, right, top, bottom, backgroundSeeds))
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

    private static Int32Rect? FindLargestSubjectBounds(
        byte[] pixels,
        int stride,
        int width,
        int height,
        IReadOnlyList<BackgroundSeed> backgroundSeeds)
    {
        var totalPixels = width * height;
        if (totalPixels <= 0)
            return null;

        var background = new bool[totalPixels];
        var queue = new Queue<int>();

        // Flood-fill only the background connected to outer edges.
        for (var x = 0; x < width; x++)
        {
            TryMarkBackground(pixels, stride, width, height, x, 0, background, queue, backgroundSeeds);
            TryMarkBackground(pixels, stride, width, height, x, height - 1, background, queue, backgroundSeeds);
        }

        for (var y = 1; y < height - 1; y++)
        {
            TryMarkBackground(pixels, stride, width, height, 0, y, background, queue, backgroundSeeds);
            TryMarkBackground(pixels, stride, width, height, width - 1, y, background, queue, backgroundSeeds);
        }

        while (queue.Count > 0)
        {
            var index = queue.Dequeue();
            var x = index % width;
            var y = index / width;

            TryMarkBackground(pixels, stride, width, height, x - 1, y, background, queue, backgroundSeeds);
            TryMarkBackground(pixels, stride, width, height, x + 1, y, background, queue, backgroundSeeds);
            TryMarkBackground(pixels, stride, width, height, x, y - 1, background, queue, backgroundSeeds);
            TryMarkBackground(pixels, stride, width, height, x, y + 1, background, queue, backgroundSeeds);
            TryMarkBackground(pixels, stride, width, height, x - 1, y - 1, background, queue, backgroundSeeds);
            TryMarkBackground(pixels, stride, width, height, x + 1, y - 1, background, queue, backgroundSeeds);
            TryMarkBackground(pixels, stride, width, height, x - 1, y + 1, background, queue, backgroundSeeds);
            TryMarkBackground(pixels, stride, width, height, x + 1, y + 1, background, queue, backgroundSeeds);
        }

        var visited = new bool[totalPixels];
        var componentQueue = new Queue<int>();
        var components = new List<SubjectComponent>();

        for (var start = 0; start < totalPixels; start++)
        {
            if (background[start] || visited[start])
                continue;

            visited[start] = true;
            componentQueue.Enqueue(start);

            var area = 0;
            var left = width;
            var right = -1;
            var top = height;
            var bottom = -1;

            while (componentQueue.Count > 0)
            {
                var index = componentQueue.Dequeue();
                var x = index % width;
                var y = index / width;

                area++;
                left = Math.Min(left, x);
                right = Math.Max(right, x);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);

                EnqueueForegroundNeighbor(index - 1, x > 0);
                EnqueueForegroundNeighbor(index + 1, x < width - 1);
                EnqueueForegroundNeighbor(index - width, y > 0);
                EnqueueForegroundNeighbor(index + width, y < height - 1);
                EnqueueForegroundNeighbor(index - width - 1, x > 0 && y > 0);
                EnqueueForegroundNeighbor(index - width + 1, x < width - 1 && y > 0);
                EnqueueForegroundNeighbor(index + width - 1, x > 0 && y < height - 1);
                EnqueueForegroundNeighbor(index + width + 1, x < width - 1 && y < height - 1);
            }

            if (area >= TrimMinimumComponentArea)
                components.Add(new SubjectComponent(left, top, right, bottom, area));
        }

        if (components.Count == 0)
            return null;

        var primary = components
            .OrderByDescending(component => ScoreComponent(component, width, height))
            .First();

        var merged = primary;
        foreach (var candidate in components)
        {
            if (candidate.Equals(primary))
                continue;

            if (IsEdgeNoise(candidate, width, height))
                continue;

            if (candidate.Area >= Math.Max(TrimMinimumComponentArea, primary.Area * TrimMergeAreaRatio) &&
                IsCloseTo(candidate, merged))
            {
                merged = merged.Merge(candidate);
            }
        }

        var bestLeft = Math.Max(0, merged.Left - TrimSafetyPadding);
        var bestTop = Math.Max(0, merged.Top - TrimSafetyPadding);
        var bestRight = Math.Min(width - 1, merged.Right + TrimSafetyPadding);
        var bestBottom = Math.Min(height - 1, merged.Bottom + TrimSafetyPadding);

        return new Int32Rect(
            bestLeft,
            bestTop,
            bestRight - bestLeft + 1,
            bestBottom - bestTop + 1);

        void EnqueueForegroundNeighbor(int neighborIndex, bool withinBounds)
        {
            if (!withinBounds || visited[neighborIndex] || background[neighborIndex])
                return;

            visited[neighborIndex] = true;
            componentQueue.Enqueue(neighborIndex);
        }
    }

    private static bool IsCloseTo(SubjectComponent candidate, SubjectComponent primary)
    {
        var horizontalGap = Math.Max(0, Math.Max(primary.Left - candidate.Right, candidate.Left - primary.Right));
        var verticalGap = Math.Max(0, Math.Max(primary.Top - candidate.Bottom, candidate.Top - primary.Bottom));

        var horizontalOverlap = Math.Min(primary.Right, candidate.Right) - Math.Max(primary.Left, candidate.Left);
        var verticalOverlap = Math.Min(primary.Bottom, candidate.Bottom) - Math.Max(primary.Top, candidate.Top);

        return (horizontalGap <= TrimMergeGap && verticalOverlap >= -12) ||
               (verticalGap <= TrimMergeGap && horizontalOverlap >= -12);
    }

    private static double ScoreComponent(SubjectComponent component, int width, int height)
    {
        if (IsEdgeNoise(component, width, height))
            return component.Area * 0.1;

        var centerX = width / 2.0;
        var centerY = height / 2.0;
        var dx = Math.Abs(component.CenterX - centerX) / Math.Max(1.0, width / 2.0);
        var dy = Math.Abs(component.CenterY - centerY) / Math.Max(1.0, height / 2.0);
        var centerBias = 1.2 - Math.Min(1.0, (dx * 0.6) + (dy * 0.9));

        return component.Area * Math.Max(0.25, centerBias);
    }

    private static bool IsEdgeNoise(SubjectComponent component, int width, int height)
    {
        if (component.Area > TrimEdgeNoiseMaxArea)
            return false;

        return component.Left <= TrimEdgeNoiseInset ||
               component.Top <= TrimEdgeNoiseInset ||
               component.Right >= width - 1 - TrimEdgeNoiseInset ||
               component.Bottom >= height - 1 - TrimEdgeNoiseInset;
    }

    private static void TryMarkBackground(
        byte[] pixels,
        int stride,
        int width,
        int height,
        int x,
        int y,
        bool[] background,
        Queue<int> queue,
        IReadOnlyList<BackgroundSeed> backgroundSeeds)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return;

        var index = (y * width) + x;
        if (background[index])
            return;

        if (!IsBackgroundPixel(pixels, (y * stride) + (x * 4), backgroundSeeds))
            return;

        background[index] = true;
        queue.Enqueue(index);
    }

    private static bool IsRemovableRow(byte[] pixels, int stride, int width, int row, IReadOnlyList<BackgroundSeed> backgroundSeeds)
    {
        var allowedContentPixels = Math.Max(2, width / 120);
        var contentCount = 0;
        var rowOffset = row * stride;

        for (var x = 0; x < width; x++)
        {
            if (IsBackgroundPixel(pixels, rowOffset + (x * 4), backgroundSeeds))
                continue;

            contentCount++;
            if (contentCount > allowedContentPixels)
                return false;
        }

        return true;
    }

    private static bool IsRemovableColumn(
        byte[] pixels,
        int stride,
        int width,
        int height,
        int column,
        int top,
        int bottom,
        IReadOnlyList<BackgroundSeed> backgroundSeeds)
    {
        var spanHeight = Math.Max(1, bottom - top + 1);
        var allowedContentPixels = Math.Max(2, spanHeight / 120);
        var contentCount = 0;

        for (var y = top; y <= bottom && y < height; y++)
        {
            if (IsBackgroundPixel(pixels, (y * stride) + (column * 4), backgroundSeeds))
                continue;

            contentCount++;
            if (contentCount > allowedContentPixels)
                return false;
        }

        return true;
    }

    private static bool IsBackgroundPixel(byte[] pixels, int offset, IReadOnlyList<BackgroundSeed> backgroundSeeds)
    {
        var blue = pixels[offset];
        var green = pixels[offset + 1];
        var red = pixels[offset + 2];
        var alpha = pixels[offset + 3];
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var luminance = (red + green + blue) / 3;
        var neutralLight = red >= NeutralBackgroundThreshold &&
                           green >= NeutralBackgroundThreshold &&
                           blue >= NeutralBackgroundThreshold &&
                           max - min <= NeutralBackgroundTolerance;

        if (alpha <= TransparentBackgroundThreshold)
            return true;

        if (red >= LightBackgroundThreshold &&
            green >= LightBackgroundThreshold &&
            blue >= LightBackgroundThreshold)
        {
            return true;
        }

        if (neutralLight)
            return true;

        if (luminance < EdgeSampleBrightnessThreshold || backgroundSeeds.Count == 0)
            return false;

        foreach (var seed in backgroundSeeds)
        {
            var dr = Math.Abs(red - seed.Red);
            var dg = Math.Abs(green - seed.Green);
            var db = Math.Abs(blue - seed.Blue);
            var dl = Math.Abs(luminance - seed.Luminance);

            if (dr <= BackgroundSeedTolerance &&
                dg <= BackgroundSeedTolerance &&
                db <= BackgroundSeedTolerance &&
                dl <= BackgroundSeedLuminanceTolerance)
            {
                return true;
            }
        }

        return false;
    }

    private static List<BackgroundSeed> BuildBackgroundSeeds(byte[] pixels, int stride, int width, int height)
    {
        var candidates = new (int X, int Y)[]
        {
            (0, 0),
            (width - 1, 0),
            (0, height - 1),
            (width - 1, height - 1),
            (width / 2, 0),
            (width / 2, height - 1),
            (0, height / 2),
            (width - 1, height / 2),
            (Math.Max(0, width / 4), 0),
            (Math.Min(width - 1, (width * 3) / 4), 0),
            (Math.Max(0, width / 4), height - 1),
            (Math.Min(width - 1, (width * 3) / 4), height - 1)
        };

        var seeds = new List<BackgroundSeed>();
        foreach (var (x, y) in candidates)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                continue;

            var offset = (y * stride) + (x * 4);
            var seed = new BackgroundSeed(
                pixels[offset + 2],
                pixels[offset + 1],
                pixels[offset],
                pixels[offset + 3]);

            if (seed.Alpha <= TransparentBackgroundThreshold)
            {
                seeds.Add(seed);
                continue;
            }

            var max = Math.Max(seed.Red, Math.Max(seed.Green, seed.Blue));
            var min = Math.Min(seed.Red, Math.Min(seed.Green, seed.Blue));
            if (seed.Luminance >= EdgeSampleBrightnessThreshold &&
                max - min <= NeutralBackgroundTolerance + 8)
            {
                seeds.Add(seed);
            }
        }

        return seeds
            .Distinct()
            .ToList();
    }

    private readonly record struct SubjectComponent(int Left, int Top, int Right, int Bottom, int Area)
    {
        public double CenterX => (Left + Right) / 2.0;
        public double CenterY => (Top + Bottom) / 2.0;

        public SubjectComponent Merge(SubjectComponent other)
        {
            return new SubjectComponent(
                Math.Min(Left, other.Left),
                Math.Min(Top, other.Top),
                Math.Max(Right, other.Right),
                Math.Max(Bottom, other.Bottom),
                Area + other.Area);
        }
    }

    private readonly record struct BackgroundSeed(byte Red, byte Green, byte Blue, byte Alpha)
    {
        public int Luminance => (Red + Green + Blue) / 3;
    }
}
