using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ClosetApp.Application.Interfaces;
using Serilog;

namespace ClosetApp.Infrastructure.Services;

public class ImageStorageService : IImageStorageService
{
    private const byte ContentAlphaThreshold = 8;
    private const int DefaultDisplayWidth = 900;
    private const int DefaultThumbnailSize = 200;

    private readonly string _appFolder;
    private readonly ICurrentUserContext? _currentUserContext;

    public ImageStorageService(string? baseFolder = null, ICurrentUserContext? currentUserContext = null)
    {
        _appFolder = string.IsNullOrWhiteSpace(baseFolder)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClosetApp")
            : baseFolder;
        _currentUserContext = currentUserContext;
        EnsureDirectories(ResolveStorageRoot());
    }

    private string ResolveStorageRoot()
    {
        if (_currentUserContext == null)
            return _appFolder;

        try
        {
            var userId = _currentUserContext.GetRequiredStoredUserIdAsync().GetAwaiter().GetResult();
            return Path.Combine(_appFolder, "users", userId.ToString("N"));
        }
        catch (InvalidOperationException)
        {
            return _appFolder;
        }
    }

    private static void EnsureDirectories(string storageRoot)
    {
        Directory.CreateDirectory(GetImageFolder(storageRoot));
        Directory.CreateDirectory(GetOriginalFolder(storageRoot));
        Directory.CreateDirectory(GetDisplayFolder(storageRoot));
        Directory.CreateDirectory(GetThumbnailFolder(storageRoot));
    }

    public async Task<string> SaveImageAsync(string sourcePath)
    {
        return await Task.Run(async () =>
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(sourcePath)}";
            var destPath = GetImageFullPath(fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            using var image = await Image.LoadAsync<Rgba32>(sourcePath);
            File.Copy(sourcePath, destPath, overwrite: true);
            CropTransparentPadding(image);
            await SaveDisplayForStoredImageAsync(image, fileName, DefaultDisplayWidth);
            await SaveThumbnailForStoredImageAsync(image, fileName, DefaultThumbnailSize);

            Log.Information("Saved clothing image {SourcePath} -> {FileName}", sourcePath, fileName);
            return fileName;
        });
    }

    public async Task<string> SaveThumbnailAsync(string sourcePath, int maxSize = 200)
    {
        var fileName = $"{Guid.NewGuid()}_thumb{Path.GetExtension(sourcePath)}";
        var destPath = Path.Combine(GetThumbnailFolder(ResolveStorageRoot()), fileName);

        using var image = await Image.LoadAsync<Rgba32>(sourcePath);
        CropTransparentPadding(image);
        await SaveResizedImageAsync(image, destPath, maxSize, CreateThumbnailEncoder);
        Log.Information("Saved thumbnail {SourcePath} -> {FileName}", sourcePath, fileName);
        return fileName;
    }

    public async Task<bool> EnsureThumbnailAsync(string imagePath, int maxSize = 200)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return false;

        var thumbnailPath = GetThumbnailFullPath(imagePath);
        if (File.Exists(thumbnailPath))
            return true;

        var imageFullPath = GetImageFullPath(imagePath);
        if (!File.Exists(imageFullPath))
            return false;

        using var image = await Image.LoadAsync<Rgba32>(imageFullPath);
        CropTransparentPadding(image);
        await SaveThumbnailForStoredImageAsync(image, imagePath, maxSize);
        Log.Information("Rebuilt missing thumbnail for {ImagePath}", imagePath);
        return true;
    }

    public async Task<bool> EnsureDisplayAsync(string imagePath, int maxWidth = DefaultDisplayWidth)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return false;

        var displayPath = GetDisplayFullPath(imagePath);
        if (File.Exists(displayPath))
            return true;

        var imageFullPath = GetImageFullPath(imagePath);
        if (!File.Exists(imageFullPath))
            return false;

        using var image = await Image.LoadAsync<Rgba32>(imageFullPath);
        CropTransparentPadding(image);
        await SaveDisplayForStoredImageAsync(image, imagePath, maxWidth);
        Log.Information("Rebuilt missing display image for {ImagePath}", imagePath);
        return true;
    }

    public async Task RestoreImageAsync(string sourcePath, string storedFileName)
    {
        await Task.Run(async () =>
        {
            var destPath = GetImageFullPath(storedFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            using var image = await Image.LoadAsync<Rgba32>(sourcePath);
            File.Copy(sourcePath, destPath, overwrite: true);
            CropTransparentPadding(image);
            await SaveDisplayForStoredImageAsync(image, storedFileName, DefaultDisplayWidth);
            await SaveThumbnailForStoredImageAsync(image, storedFileName, DefaultThumbnailSize);

            Log.Information("Restored clothing image {SourcePath} -> {FileName}", sourcePath, storedFileName);
        });
    }

    private async Task SaveDisplayForStoredImageAsync(Image<Rgba32> image, string imageFileName, int maxWidth)
    {
        var displayPath = GetDisplayFullPath(imageFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(displayPath)!);
        await SaveResizedImageAsync(image, displayPath, maxWidth, CreateDisplayEncoder);
    }

    private async Task SaveThumbnailForStoredImageAsync(Image<Rgba32> image, string imageFileName, int maxSize)
    {
        var thumbnailPath = GetThumbnailFullPath(imageFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);
        await SaveResizedImageAsync(image, thumbnailPath, maxSize, CreateThumbnailEncoder);
    }

    private static async Task SaveResizedImageAsync(
        Image<Rgba32> image,
        string destinationPath,
        int maxSize,
        Func<string, bool, IImageEncoder> createEncoder)
    {
        using var resized = image.Clone();
        resized.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(maxSize, maxSize),
            Mode = ResizeMode.Max
        }));

        resized.Metadata.ExifProfile = null;
        resized.Metadata.IccProfile = null;
        resized.Metadata.XmpProfile = null;

        var encoder = createEncoder(destinationPath, HasVisibleTransparency(resized));
        await resized.SaveAsync(destinationPath, encoder);
    }

    private static void CropTransparentPadding(Image<Rgba32> image)
    {
        var bounds = FindContentBounds(image);
        if (bounds == null)
            return;

        var rect = bounds.Value;
        if (rect.X == 0 && rect.Y == 0 && rect.Width == image.Width && rect.Height == image.Height)
            return;

        image.Mutate(x => x.Crop(rect));
    }

    private static Rectangle? FindContentBounds(Image<Rgba32> image)
    {
        var left = image.Width;
        var top = image.Height;
        var right = -1;
        var bottom = -1;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (row[x].A <= ContentAlphaThreshold)
                        continue;

                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x);
                    bottom = Math.Max(bottom, y);
                }
            }
        });

        if (right < left || bottom < top)
            return null;

        return new Rectangle(left, top, right - left + 1, bottom - top + 1);
    }

    public async Task DeleteImageAsync(string imagePath)
    {
        await DeleteImageWithThumbnailAsync(imagePath);
    }

    public Task DeleteImageWithThumbnailAsync(string imagePath)
    {
        foreach (var fullPath in EnumerateVariantPaths(imagePath))
        {
            if (!File.Exists(fullPath))
                continue;

            File.Delete(fullPath);
            Log.Information("Deleted clothing image asset {ImagePath}", fullPath);
        }
        return Task.CompletedTask;
    }

    public async Task TryDeleteImageAsync(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        try
        {
            await DeleteImageWithThumbnailAsync(imagePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to delete stored clothing image {ImagePath}", imagePath);
        }
    }

    public string GetImageFullPath(string relativePath)
    {
        return Path.Combine(GetOriginalFolder(ResolveStorageRoot()), relativePath);
    }

    public string GetDisplayFullPath(string relativePath)
    {
        return Path.Combine(GetDisplayFolder(ResolveStorageRoot()), relativePath);
    }

    public string GetThumbnailFullPath(string relativePath)
    {
        var name = Path.GetFileNameWithoutExtension(relativePath);
        var ext = Path.GetExtension(relativePath);
        return Path.Combine(GetThumbnailFolder(ResolveStorageRoot()), $"{name}_thumb{ext}");
    }

    public IReadOnlyList<string> GetOriginalImageFullPaths()
    {
        var originalFolder = GetOriginalFolder(ResolveStorageRoot());
        if (!Directory.Exists(originalFolder))
            return [];

        return Directory.EnumerateFiles(originalFolder, "*", SearchOption.TopDirectoryOnly)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> GetImageAssetFullPaths(string relativePath)
    {
        return EnumerateVariantPaths(relativePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IEnumerable<string> EnumerateVariantPaths(string relativePath)
    {
        var name = Path.GetFileNameWithoutExtension(relativePath);
        var ext = Path.GetExtension(relativePath);
        var storageRoot = ResolveStorageRoot();
        yield return Path.Combine(GetOriginalFolder(storageRoot), relativePath);
        yield return Path.Combine(GetDisplayFolder(storageRoot), relativePath);
        yield return Path.Combine(GetThumbnailFolder(storageRoot), $"{name}_thumb{ext}");
    }

    private static string GetImageFolder(string storageRoot) => Path.Combine(storageRoot, "images");

    private static string GetOriginalFolder(string storageRoot) => Path.Combine(GetImageFolder(storageRoot), "originals");

    private static string GetDisplayFolder(string storageRoot) => Path.Combine(GetImageFolder(storageRoot), "display");

    private static string GetThumbnailFolder(string storageRoot) => Path.Combine(GetImageFolder(storageRoot), "thumbnails");

    private static IImageEncoder CreateDisplayEncoder(string destinationPath, bool hasTransparency)
    {
        return Path.GetExtension(destinationPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => new JpegEncoder
            {
                Quality = 90
            },
            ".png" => new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.Level6,
                ColorType = hasTransparency ? PngColorType.RgbWithAlpha : PngColorType.Rgb
            },
            ".gif" => new GifEncoder(),
            ".bmp" => new BmpEncoder(),
            _ => new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.Level6,
                ColorType = hasTransparency ? PngColorType.RgbWithAlpha : PngColorType.Rgb
            }
        };
    }

    private static IImageEncoder CreateThumbnailEncoder(string destinationPath, bool hasTransparency)
    {
        return Path.GetExtension(destinationPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => new JpegEncoder
            {
                Quality = 82
            },
            ".png" => new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                ColorType = hasTransparency ? PngColorType.RgbWithAlpha : PngColorType.Rgb
            },
            ".gif" => new GifEncoder(),
            ".bmp" => new BmpEncoder(),
            _ => new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                ColorType = hasTransparency ? PngColorType.RgbWithAlpha : PngColorType.Rgb
            }
        };
    }

    private static bool HasVisibleTransparency(Image<Rgba32> image)
    {
        var hasTransparency = false;
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height && !hasTransparency; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    if (row[x].A < byte.MaxValue)
                    {
                        hasTransparency = true;
                        break;
                    }
                }
            }
        });

        return hasTransparency;
    }
}
