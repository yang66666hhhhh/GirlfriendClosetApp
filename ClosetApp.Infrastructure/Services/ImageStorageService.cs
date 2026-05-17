using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Serilog;

namespace ClosetApp.Infrastructure.Services;

public class ImageStorageService : IImageStorageService
{
    private const byte ContentAlphaThreshold = 8;
    private const int DefaultThumbnailSize = 200;

    private readonly string _imageFolder;
    private readonly string _thumbnailFolder;

    public ImageStorageService(string? baseFolder = null)
    {
        var appFolder = string.IsNullOrWhiteSpace(baseFolder)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClosetApp")
            : baseFolder;
        _imageFolder = Path.Combine(appFolder, "images");
        _thumbnailFolder = Path.Combine(appFolder, "thumbnails");
        Directory.CreateDirectory(_imageFolder);
        Directory.CreateDirectory(_thumbnailFolder);
    }

    public async Task<string> SaveImageAsync(string sourcePath)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(sourcePath)}";
        var destPath = Path.Combine(_imageFolder, fileName);

        using var image = await Image.LoadAsync<Rgba32>(sourcePath);
        CropTransparentPadding(image);
        await image.SaveAsync(destPath);
        await SaveThumbnailForStoredImageAsync(image, fileName, DefaultThumbnailSize);

        Log.Information("Saved clothing image {SourcePath} -> {FileName}", sourcePath, fileName);
        return fileName;
    }

    public async Task<string> SaveThumbnailAsync(string sourcePath, int maxSize = 200)
    {
        var fileName = $"{Guid.NewGuid()}_thumb{Path.GetExtension(sourcePath)}";
        var destPath = Path.Combine(_thumbnailFolder, fileName);
        
        using var image = await Image.LoadAsync<Rgba32>(sourcePath);
        CropTransparentPadding(image);
        await SaveThumbnailImageAsync(image, destPath, maxSize);
        Log.Information("Saved thumbnail {SourcePath} -> {FileName}", sourcePath, fileName);
        return fileName;
    }

    private async Task SaveThumbnailForStoredImageAsync(Image<Rgba32> image, string imageFileName, int maxSize)
    {
        var thumbnailPath = GetThumbnailFullPath(imageFileName);
        await SaveThumbnailImageAsync(image, thumbnailPath, maxSize);
    }

    private static async Task SaveThumbnailImageAsync(Image<Rgba32> image, string destinationPath, int maxSize)
    {
        using var thumbnail = image.Clone();
        thumbnail.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(maxSize, maxSize),
            Mode = ResizeMode.Max
        }));
        await thumbnail.SaveAsync(destinationPath);
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

    public Task DeleteImageAsync(string imagePath)
    {
        var fullPath = GetImageFullPath(imagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Log.Information("Deleted clothing image {ImagePath}", fullPath);
        }
        return Task.CompletedTask;
    }

    public Task DeleteImageWithThumbnailAsync(string imagePath)
    {
        var fullPath = GetImageFullPath(imagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Log.Information("Deleted clothing image {ImagePath}", fullPath);
        }
        var thumbPath = GetThumbnailFullPath(imagePath);
        if (File.Exists(thumbPath))
        {
            File.Delete(thumbPath);
            Log.Information("Deleted clothing thumbnail {ThumbnailPath}", thumbPath);
        }
        return Task.CompletedTask;
    }

    public string GetImageFullPath(string relativePath)
    {
        return Path.Combine(_imageFolder, relativePath);
    }

    public string GetThumbnailFullPath(string relativePath)
    {
        var name = Path.GetFileNameWithoutExtension(relativePath);
        var ext = Path.GetExtension(relativePath);
        return Path.Combine(_thumbnailFolder, $"{name}_thumb{ext}");
    }
}
