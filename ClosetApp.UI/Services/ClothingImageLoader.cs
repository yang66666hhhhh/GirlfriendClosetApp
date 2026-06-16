using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClosetApp.Application.Images;

namespace ClosetApp.UI.Services;

public static class ClothingImageLoader
{
    private const int MaxImageCacheEntries = 240;
    private const int MaxSizeCacheEntries = 512;
    private const int MaxFailureCacheEntries = 256;
    private const byte TransparentBackgroundThreshold = 12;
    private const byte LightBackgroundThreshold = 240;
    private const byte NeutralBackgroundThreshold = 232;
    private const byte NeutralBackgroundTolerance = 15;
    private const byte EdgeSampleBrightnessThreshold = 150;
    private const int TrimSafetyPadding = 8;
    private const int TrimMinimumComponentArea = 250;
    private const double TrimMergeAreaRatio = 0.18;
    private const int TrimMergeGap = 24;
    private const int TrimEdgeNoiseInset = 12;
    private const int TrimEdgeNoiseMaxArea = 1400;
    private const int BackgroundSeedTolerance = 10;
    private const int BackgroundSeedLuminanceTolerance = 10;
    private const int ForegroundProtectionLuminanceGap = 55;
    private const byte NeutralClothingMin = 60;
    private const byte NeutralClothingMax = 220;
    private const byte NeutralClothingSatMax = 35;
    private const int NeutralClothingSeedTight = 6;
    private const double TallSilhouetteThreshold = 0.72;
    private const double VeryTallSilhouetteThreshold = 0.88;
    private const double TallSilhouetteMinWidthRatio = 0.54;
    private const double VeryTallSilhouetteMinWidthRatio = 0.48;
    private const double TallSilhouetteExtraHeightRatio = 0.04;
    private static readonly TimeSpan FailureCacheTtl = TimeSpan.FromSeconds(45);

    private static readonly string ImagesFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClosetApp", "images");
    private static readonly string OriginalFolder = Path.Combine(ImagesFolder, "originals");
    private static readonly string DisplayFolder = Path.Combine(ImagesFolder, "display");
    private static readonly string ThumbnailFolder = Path.Combine(ImagesFolder, "thumbnails");

    // 用户作用域目录，启动时由 Configure() 设置；未设置时回退到全局目录。
    private static string? _scopedOriginalFolder;
    private static string? _scopedDisplayFolder;
    private static string? _scopedThumbnailFolder;

    /// <summary>
    /// 设置用户作用域的图片目录。调用后 ResolvePath 会优先在用户目录中查找，
    /// 找不到时仍回退到全局目录。
    /// </summary>
    public static void Configure(string originalsDir, string displayDir, string thumbnailsDir)
    {
        _scopedOriginalFolder = originalsDir;
        _scopedDisplayFolder = displayDir;
        _scopedThumbnailFolder = thumbnailsDir;
        ClearMemoryCaches();
    }

    private static readonly ConcurrentDictionary<string, WeakReference<ImageSource>> ImageCache = new();
    private static readonly ConcurrentDictionary<string, Size?> SizeCache = new();
    private static readonly ConcurrentDictionary<string, DateTimeOffset> FailedImageCache = new();
    private static readonly ConcurrentDictionary<string, Task<ImageSource?>> InflightImageLoads = new();
    private static readonly ConcurrentDictionary<string, Task<Size?>> InflightSizeLoads = new();
    private static readonly ConcurrentQueue<string> ImageCacheOrder = new();
    private static readonly ConcurrentQueue<string> SizeCacheOrder = new();
    private static readonly ConcurrentQueue<string> FailureCacheOrder = new();

    public static ImageSource? Load(string? path, int decodePixelWidth = 400)
    {
        return Load(path, ImageVariant.Display, decodePixelWidth);
    }

    public static ImageSource? Load(
        string? path,
        ImageVariant variant,
        int decodePixelWidth = 400,
        bool trimLightPadding = false,
        bool extractForeground = false)
    {
        var resolved = ResolvePath(path, variant);
        if (resolved == null)
            return null;

        var key = BuildCacheKey(resolved, decodePixelWidth, trimLightPadding, extractForeground);
        if (TryGetCachedImage(key, out var cached))
            return cached;
        if (HasRecentFailure(key))
            return null;

        var image = LoadCore(resolved, decodePixelWidth, trimLightPadding, extractForeground);
        if (image != null)
        {
            ClearFailedLoad(key);
            CacheImage(key, image);
        }
        else
        {
            CacheFailedLoad(key);
        }

        return image;
    }

    public static Task<ImageSource?> LoadAsync(
        string? path,
        ImageVariant variant,
        int decodePixelWidth = 400,
        bool trimLightPadding = false,
        bool extractForeground = false,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolvePath(path, variant);
        if (resolved == null)
            return Task.FromResult<ImageSource?>(null);

        var key = BuildCacheKey(resolved, decodePixelWidth, trimLightPadding, extractForeground);
        if (TryGetCachedImage(key, out var cached))
            return Task.FromResult(cached);
        if (HasRecentFailure(key))
            return Task.FromResult<ImageSource?>(null);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult<ImageSource?>(null);

        var loadTask = InflightImageLoads.GetOrAdd(key, _ => Task.Run(() =>
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            var image = LoadCore(resolved, decodePixelWidth, trimLightPadding, extractForeground);
            if (cancellationToken.IsCancellationRequested)
                return null;

            if (image != null)
            {
                ClearFailedLoad(key);
                CacheImage(key, image);
            }
            else
            {
                CacheFailedLoad(key);
            }

            return image;
        }, cancellationToken));

        return AwaitInflightLoadAsync(key, loadTask, cancellationToken);
    }

    public static Size? GetDisplaySize(string? path, int decodePixelWidth = 400, bool trimLightPadding = false)
    {
        var resolved = ResolvePath(path, ImageVariant.Display);
        if (resolved == null)
            return null;

        var key = BuildCacheKey(resolved, decodePixelWidth, trimLightPadding, extractForeground: false);
        if (SizeCache.TryGetValue(key, out var cachedSize))
            return cachedSize;
        if (HasRecentFailure(key))
            return null;

        var sizeTask = InflightSizeLoads.GetOrAdd(key, _ => Task.Run<Size?>(() =>
        {
            var image = LoadCore(resolved, decodePixelWidth, trimLightPadding, extractForeground: false);
            return image == null ? null : new Size(image.Width, image.Height);
        }));

        var size = sizeTask.GetAwaiter().GetResult();
        if (size != null)
        {
            ClearFailedLoad(key);
            SizeCache[key] = size;
            CacheSizeKey(key);
        }
        else
        {
            CacheFailedLoad(key);
        }

        if (sizeTask.IsCompleted)
            InflightSizeLoads.TryRemove(key, out _);
        return size;
    }

    public static void ClearMemoryCaches()
    {
        ImageCache.Clear();
        SizeCache.Clear();
        FailedImageCache.Clear();
        InflightImageLoads.Clear();
        InflightSizeLoads.Clear();

        while (ImageCacheOrder.TryDequeue(out _))
        {
        }

        while (SizeCacheOrder.TryDequeue(out _))
        {
        }

        while (FailureCacheOrder.TryDequeue(out _))
        {
        }
    }

    internal static bool HasRecentFailureForDiagnostics(
        string? path,
        ImageVariant variant,
        int decodePixelWidth = 400,
        bool trimLightPadding = false,
        bool extractForeground = false)
    {
        var resolved = ResolvePath(path, variant);
        if (resolved == null)
            return false;

        var key = BuildCacheKey(resolved, decodePixelWidth, trimLightPadding, extractForeground);
        return HasRecentFailure(key);
    }

    internal static bool HasSizeCacheEntryForDiagnostics(
        string? path,
        int decodePixelWidth = 400,
        bool trimLightPadding = false)
    {
        var resolved = ResolvePath(path, ImageVariant.Display);
        if (resolved == null)
            return false;

        var key = BuildCacheKey(resolved, decodePixelWidth, trimLightPadding, extractForeground: false);
        return SizeCache.ContainsKey(key);
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

        // 优先在用户作用域目录中查找（按 LocalUserId 隔离）。
        if (_scopedOriginalFolder != null)
        {
            var scopedOriginal = Path.Combine(_scopedOriginalFolder, path);
            var scopedDisplay = Path.Combine(_scopedDisplayFolder!, path);
            var scopedThumbnail = BuildThumbnailPath(_scopedThumbnailFolder!, path);

            var scopedResult = variant switch
            {
                ImageVariant.Original => FirstExisting(scopedOriginal, scopedDisplay, scopedThumbnail),
                ImageVariant.Thumbnail => FirstExisting(scopedThumbnail, scopedDisplay, scopedOriginal),
                _ => FirstExisting(scopedDisplay, scopedOriginal, scopedThumbnail)
            };

            if (scopedResult != null)
                return scopedResult;
        }

        // 回退到全局目录（兼容旧数据和未配置场景）。
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

    private static ImageSource? LoadCore(string path, int decodePixelWidth, bool trimLightPadding, bool extractForeground)
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

            if (!trimLightPadding && !extractForeground)
                return bitmap;

            return extractForeground
                ? TryExtractForeground(bitmap, trimLightPadding)
                : TryTrimLightPadding(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildCacheKey(string path, int decodePixelWidth, bool trimLightPadding, bool extractForeground)
    {
        var ticks = File.Exists(path)
            ? File.GetLastWriteTimeUtc(path).Ticks
            : 0L;
        return $"{path}|{ticks}|{decodePixelWidth}|trim:{trimLightPadding}|fg:{extractForeground}";
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

    private static bool TryGetCachedImage(string key, out ImageSource? image)
    {
        image = null;
        if (!ImageCache.TryGetValue(key, out var weakReference))
            return false;

        if (weakReference.TryGetTarget(out image))
            return true;

        ImageCache.TryRemove(key, out _);
        return false;
    }

    private static void CacheImage(string key, ImageSource image)
    {
        ImageCache[key] = new WeakReference<ImageSource>(image);
        ImageCacheOrder.Enqueue(key);
        TrimImageCache();
    }

    private static void CacheSizeKey(string key)
    {
        SizeCacheOrder.Enqueue(key);
        TrimSizeCache();
    }

    private static bool HasRecentFailure(string key)
    {
        if (!FailedImageCache.TryGetValue(key, out var expiresAt))
            return false;

        if (expiresAt > DateTimeOffset.UtcNow)
            return true;

        FailedImageCache.TryRemove(key, out _);
        return false;
    }

    private static void CacheFailedLoad(string key)
    {
        FailedImageCache[key] = DateTimeOffset.UtcNow.Add(FailureCacheTtl);
        FailureCacheOrder.Enqueue(key);
        TrimFailureCache();
    }

    private static void ClearFailedLoad(string key)
    {
        FailedImageCache.TryRemove(key, out _);
    }

    private static async Task<ImageSource?> AwaitInflightLoadAsync(
        string key,
        Task<ImageSource?> loadTask,
        CancellationToken cancellationToken)
    {
        try
        {
            return await loadTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        finally
        {
            if (loadTask.IsCompleted)
                InflightImageLoads.TryRemove(key, out _);
        }
    }

    private static void TrimImageCache()
    {
        while (ImageCache.Count > MaxImageCacheEntries && ImageCacheOrder.TryDequeue(out var key))
        {
            if (ImageCache.TryGetValue(key, out var weakReference) &&
                !weakReference.TryGetTarget(out _))
            {
                ImageCache.TryRemove(key, out _);
                continue;
            }

            ImageCache.TryRemove(key, out _);
        }
    }

    private static void TrimSizeCache()
    {
        while (SizeCache.Count > MaxSizeCacheEntries && SizeCacheOrder.TryDequeue(out var key))
            SizeCache.TryRemove(key, out _);
    }

    private static void TrimFailureCache()
    {
        var now = DateTimeOffset.UtcNow;
        while (FailedImageCache.Count > MaxFailureCacheEntries && FailureCacheOrder.TryDequeue(out var key))
        {
            if (FailedImageCache.TryGetValue(key, out var expiresAt) && expiresAt > now)
            {
                FailedImageCache.TryRemove(key, out _);
                continue;
            }

            FailedImageCache.TryRemove(key, out _);
        }
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

    private static ImageSource TryExtractForeground(BitmapSource source, bool trimLightPadding)
    {
        try
        {
            var normalized = source.Format == PixelFormats.Bgra32
                ? source
                : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            if (normalized.CanFreeze && !normalized.IsFrozen)
                normalized.Freeze();

            var width = normalized.PixelWidth;
            var height = normalized.PixelHeight;
            if (width <= 0 || height <= 0)
                return source;

            var stride = ((width * normalized.Format.BitsPerPixel) + 7) / 8;
            var pixels = new byte[stride * height];
            normalized.CopyPixels(pixels, stride, 0);

            var backgroundSeeds = BuildBackgroundSeeds(pixels, stride, width, height);
            var backgroundMask = BuildConnectedBackgroundMask(pixels, stride, width, height, backgroundSeeds);
            var bounds = FindLargestSubjectBounds(pixels, stride, width, height, backgroundSeeds, backgroundMask)
                         ?? (trimLightPadding ? FindLightPaddingBounds(normalized) : null);

            var mutated = false;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (!backgroundMask[(y * width) + x])
                        continue;

                    var offset = (y * stride) + (x * 4);
                    if (pixels[offset + 3] == 0)
                        continue;

                    pixels[offset + 3] = 0;
                    mutated = true;
                }
            }

            BitmapSource result;
            if (mutated)
            {
                var isolated = BitmapSource.Create(
                    width,
                    height,
                    normalized.DpiX,
                    normalized.DpiY,
                    PixelFormats.Bgra32,
                    null,
                    pixels,
                    stride);
                isolated.Freeze();
                result = isolated;
            }
            else
            {
                result = normalized;
            }

            if (bounds == null)
                return result;

            var rect = bounds.Value;
            if (rect.X == 0 && rect.Y == 0 && rect.Width == width && rect.Height == height)
                return result;

            var cropped = new CroppedBitmap(result, rect);
            cropped.Freeze();
            return cropped;
        }
        catch
        {
            return trimLightPadding ? TryTrimLightPadding(source) : source;
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

        return NormalizeBounds(left, top, right, bottom, source.PixelWidth, source.PixelHeight);
    }

    private static Int32Rect? FindLargestSubjectBounds(
        byte[] pixels,
        int stride,
        int width,
        int height,
        IReadOnlyList<BackgroundSeed> backgroundSeeds,
        bool[]? backgroundMask = null)
    {
        var totalPixels = width * height;
        if (totalPixels <= 0)
            return null;

        var background = backgroundMask ?? BuildConnectedBackgroundMask(pixels, stride, width, height, backgroundSeeds);

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

        return NormalizeBounds(merged.Left, merged.Top, merged.Right, merged.Bottom, width, height);

        void EnqueueForegroundNeighbor(int neighborIndex, bool withinBounds)
        {
            if (!withinBounds || visited[neighborIndex] || background[neighborIndex])
                return;

            visited[neighborIndex] = true;
            componentQueue.Enqueue(neighborIndex);
        }
    }

    private static bool[] BuildConnectedBackgroundMask(
        byte[] pixels,
        int stride,
        int width,
        int height,
        IReadOnlyList<BackgroundSeed> backgroundSeeds)
    {
        var background = new bool[width * height];
        var queue = new Queue<int>();

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

        return background;
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

    private static Int32Rect? NormalizeBounds(int left, int top, int right, int bottom, int width, int height)
    {
        left = Math.Max(0, left - TrimSafetyPadding);
        top = Math.Max(0, top - TrimSafetyPadding);
        right = Math.Min(width - 1, right + TrimSafetyPadding);
        bottom = Math.Min(height - 1, bottom + TrimSafetyPadding);

        var currentWidth = right - left + 1;
        var currentHeight = bottom - top + 1;
        if (currentWidth <= 0 || currentHeight <= 0)
            return null;

        var heightRatio = currentHeight / (double)Math.Max(1, height);
        var targetMinWidthRatio = heightRatio >= VeryTallSilhouetteThreshold
            ? VeryTallSilhouetteMinWidthRatio
            : heightRatio >= TallSilhouetteThreshold
                ? TallSilhouetteMinWidthRatio
                : 0;

        if (targetMinWidthRatio > 0)
        {
            var targetWidth = (int)Math.Ceiling(width * targetMinWidthRatio);
            if (currentWidth < targetWidth)
            {
                var center = (left + right) / 2.0;
                left = Math.Max(0, (int)Math.Floor(center - (targetWidth / 2.0)));
                right = Math.Min(width - 1, left + targetWidth - 1);

                if (right - left + 1 < targetWidth)
                    left = Math.Max(0, right - targetWidth + 1);
            }

            var extraHeight = (int)Math.Round(height * TallSilhouetteExtraHeightRatio);
            top = Math.Max(0, top - extraHeight);
            bottom = Math.Min(height - 1, bottom + extraHeight);
        }

        currentWidth = right - left + 1;
        currentHeight = bottom - top + 1;
        if (currentWidth <= 0 || currentHeight <= 0)
            return null;

        return new Int32Rect(left, top, currentWidth, currentHeight);
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
        var saturation = max - min;
        var luminance = (red + green + blue) / 3;
        var neutralLight = red >= NeutralBackgroundThreshold &&
                           green >= NeutralBackgroundThreshold &&
                           blue >= NeutralBackgroundThreshold &&
                           saturation <= NeutralBackgroundTolerance;

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

        if (luminance >= NeutralClothingMin &&
            luminance <= NeutralClothingMax &&
            saturation <= NeutralClothingSatMax)
        {
            if (backgroundSeeds.Count > 0)
            {
                var avgSeedLum = (int)backgroundSeeds.Average(s => s.Luminance);
                if (Math.Abs(luminance - avgSeedLum) > ForegroundProtectionLuminanceGap)
                    return false;

                var tightMatch = true;
                foreach (var seed in backgroundSeeds)
                {
                    if (Math.Abs(red - seed.Red) > NeutralClothingSeedTight ||
                        Math.Abs(green - seed.Green) > NeutralClothingSeedTight ||
                        Math.Abs(blue - seed.Blue) > NeutralClothingSeedTight)
                    {
                        tightMatch = false;
                        break;
                    }
                }
                if (!tightMatch)
                    return false;
            }
            else
            {
                return false;
            }
        }

        if (backgroundSeeds.Count == 0)
            return false;

        var avgSeedLuminance = (int)backgroundSeeds.Average(s => s.Luminance);
        if (luminance < avgSeedLuminance - ForegroundProtectionLuminanceGap)
            return false;

        var neutralEnough = saturation <= NeutralBackgroundTolerance + 6;
        if (!neutralEnough && luminance < LightBackgroundThreshold)
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
                dl <= BackgroundSeedLuminanceTolerance &&
                (neutralEnough || luminance >= EdgeSampleBrightnessThreshold))
            {
                return true;
            }
        }

        return false;
    }

    private static List<BackgroundSeed> BuildBackgroundSeeds(byte[] pixels, int stride, int width, int height)
    {
        var seeds = new List<BackgroundSeed>();

        foreach (var (x, y) in EnumerateEdgeSamples(width, height))
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
                max - min <= NeutralBackgroundTolerance)
            {
                seeds.Add(seed);
            }
        }

        return seeds
            .Distinct()
            .ToList();
    }

    private static IEnumerable<(int X, int Y)> EnumerateEdgeSamples(int width, int height)
    {
        var xStops = new[]
        {
            0,
            Math.Max(0, width / 6),
            Math.Max(0, width / 3),
            width / 2,
            Math.Min(width - 1, (width * 2) / 3),
            Math.Min(width - 1, (width * 5) / 6),
            width - 1
        }
        .Distinct();

        var yStops = new[]
        {
            0,
            Math.Max(0, height / 6),
            Math.Max(0, height / 3),
            height / 2,
            Math.Min(height - 1, (height * 2) / 3),
            Math.Min(height - 1, (height * 5) / 6),
            height - 1
        }
        .Distinct();

        foreach (var x in xStops)
        {
            yield return (x, 0);
            yield return (x, height - 1);
        }

        foreach (var y in yStops)
        {
            yield return (0, y);
            yield return (width - 1, y);
        }
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
