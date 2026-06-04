using ClosetApp.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ClosetApp.Infrastructure.Services;

public sealed class AiAssetStorageService : IAiAssetStorageService
{
    private const int DisplayWidth = 900;
    private const int ThumbnailSize = 200;

    public async Task<string> SaveProfileReferenceImageAsync(string sourcePath, string slotName)
    {
        var extension = NormalizeExtension(Path.GetExtension(sourcePath));
        var storedFileName = $"{slotName}{extension}";
        var destinationPath = Path.Combine(AppPaths.AiProfileDir, storedFileName);
        Directory.CreateDirectory(AppPaths.AiProfileDir);
        File.Copy(sourcePath, destinationPath, overwrite: true);
        return storedFileName;
    }

    public async Task<string> SaveGeneratedImageAsync(byte[] bytes, string mimeType)
    {
        var extension = GetExtensionFromMimeType(mimeType);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var originalPath = Path.Combine(AppPaths.AiRendersOriginalsDir, storedFileName);
        var displayPath = Path.Combine(AppPaths.AiRendersDisplayDir, storedFileName);
        var thumbnailPath = BuildThumbnailPath(storedFileName);

        await File.WriteAllBytesAsync(originalPath, bytes);

        await using var memoryStream = new MemoryStream(bytes);
        using var image = await Image.LoadAsync<Rgba32>(memoryStream);
        await SaveVariantAsync(image, displayPath, DisplayWidth);
        await SaveVariantAsync(image, thumbnailPath, ThumbnailSize);
        return storedFileName;
    }

    public Task RestoreProfileReferenceImageAsync(string sourcePath, string storedFileName)
    {
        var destinationPath = Path.Combine(AppPaths.AiProfileDir, storedFileName);
        Directory.CreateDirectory(AppPaths.AiProfileDir);
        File.Copy(sourcePath, destinationPath, overwrite: true);
        return Task.CompletedTask;
    }

    public async Task RestoreGeneratedImageAsync(string sourcePath, string storedFileName)
    {
        var bytes = await File.ReadAllBytesAsync(sourcePath);
        var originalPath = Path.Combine(AppPaths.AiRendersOriginalsDir, storedFileName);
        await File.WriteAllBytesAsync(originalPath, bytes);

        await using var memoryStream = new MemoryStream(bytes);
        using var image = await Image.LoadAsync<Rgba32>(memoryStream);
        await SaveVariantAsync(image, Path.Combine(AppPaths.AiRendersDisplayDir, storedFileName), DisplayWidth);
        await SaveVariantAsync(image, BuildThumbnailPath(storedFileName), ThumbnailSize);
    }

    public Task TryDeleteProfileReferenceImageAsync(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return Task.CompletedTask;

        var fullPath = GetProfileReferenceFullPath(imagePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task TryDeleteGeneratedImageAsync(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return Task.CompletedTask;

        foreach (var assetPath in GetGeneratedImageAssetFullPaths(imagePath))
        {
            if (File.Exists(assetPath))
                File.Delete(assetPath);
        }

        return Task.CompletedTask;
    }

    public string GetProfileReferenceFullPath(string relativePath)
    {
        return Path.Combine(AppPaths.AiProfileDir, relativePath);
    }

    public string GetGeneratedImageFullPath(string relativePath)
    {
        return Path.Combine(AppPaths.AiRendersOriginalsDir, relativePath);
    }

    public IReadOnlyList<string> GetGeneratedImageAssetFullPaths(string relativePath)
    {
        return
        [
            GetGeneratedImageFullPath(relativePath),
            Path.Combine(AppPaths.AiRendersDisplayDir, relativePath),
            BuildThumbnailPath(relativePath)
        ];
    }

    private static async Task SaveVariantAsync(Image<Rgba32> image, string path, int maxSize)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var clone = image.Clone();
        clone.Mutate(operation => operation.Resize(new ResizeOptions
        {
            Size = new Size(maxSize, maxSize),
            Mode = ResizeMode.Max
        }));

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is ".jpg" or ".jpeg")
        {
            await clone.SaveAsync(path, new JpegEncoder { Quality = 90 });
            return;
        }

        await clone.SaveAsync(path, new PngEncoder());
    }

    private static string BuildThumbnailPath(string storedFileName)
    {
        var fileName = Path.GetFileNameWithoutExtension(storedFileName);
        var extension = Path.GetExtension(storedFileName);
        return Path.Combine(AppPaths.AiRendersThumbnailsDir, $"{fileName}_thumb{extension}");
    }

    private static string NormalizeExtension(string extension)
    {
        return string.IsNullOrWhiteSpace(extension) ? ".png" : extension.ToLowerInvariant();
    }

    private static string GetExtensionFromMimeType(string mimeType)
    {
        return mimeType.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/webp" => ".webp",
            _ => ".png"
        };
    }
}
