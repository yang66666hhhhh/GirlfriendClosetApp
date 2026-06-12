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
    private readonly string _appFolder;
    private readonly ICurrentUserContext? _currentUserContext;

    public AiAssetStorageService(string? baseFolder = null, ICurrentUserContext? currentUserContext = null)
    {
        _appFolder = string.IsNullOrWhiteSpace(baseFolder) ? AppPaths.BaseDir : baseFolder;
        _currentUserContext = currentUserContext;
        EnsureDirectories(ResolveStorageRoot());
    }

    public async Task<string> SaveProfileReferenceImageAsync(string sourcePath, string slotName, Guid? userId = null)
    {
        const string extension = ".png";
        var storedFileName = $"{slotName}{extension}";
        var destinationPath = Path.Combine(GetAiProfileDir(ResolveStorageRoot(userId)), storedFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        using var image = await Image.LoadAsync<Rgba32>(sourcePath);
        await image.SaveAsync(destinationPath, new PngEncoder());

        return storedFileName;
    }

    public async Task<string> SaveGeneratedImageAsync(byte[] bytes, string mimeType)
    {
        var extension = GetExtensionFromMimeType(mimeType);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storageRoot = ResolveStorageRoot();
        var originalPath = Path.Combine(GetAiRendersOriginalsDir(storageRoot), storedFileName);
        var displayPath = Path.Combine(GetAiRendersDisplayDir(storageRoot), storedFileName);
        var thumbnailPath = BuildThumbnailPath(storedFileName);

        await File.WriteAllBytesAsync(originalPath, bytes);

        await using var memoryStream = new MemoryStream(bytes);
        using var image = await Image.LoadAsync<Rgba32>(memoryStream);
        await SaveVariantAsync(image, displayPath, DisplayWidth);
        await SaveVariantAsync(image, thumbnailPath, ThumbnailSize);
        return storedFileName;
    }

    public Task RestoreProfileReferenceImageAsync(string sourcePath, string storedFileName, Guid? userId = null)
    {
        var destinationPath = Path.Combine(GetAiProfileDir(ResolveStorageRoot(userId)), storedFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourcePath, destinationPath, overwrite: true);
        return Task.CompletedTask;
    }

    public async Task RestoreGeneratedImageAsync(string sourcePath, string storedFileName)
    {
        var bytes = await File.ReadAllBytesAsync(sourcePath);
        var storageRoot = ResolveStorageRoot();
        var originalPath = Path.Combine(GetAiRendersOriginalsDir(storageRoot), storedFileName);
        await File.WriteAllBytesAsync(originalPath, bytes);

        await using var memoryStream = new MemoryStream(bytes);
        using var image = await Image.LoadAsync<Rgba32>(memoryStream);
        await SaveVariantAsync(image, Path.Combine(GetAiRendersDisplayDir(storageRoot), storedFileName), DisplayWidth);
        await SaveVariantAsync(image, BuildThumbnailPath(storedFileName), ThumbnailSize);
    }

    public Task TryDeleteProfileReferenceImageAsync(string? imagePath, Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return Task.CompletedTask;

        var fullPath = GetProfileReferenceFullPath(imagePath, userId);
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

    public string GetProfileReferenceFullPath(string relativePath, Guid? userId = null)
    {
        return Path.Combine(GetAiProfileDir(ResolveStorageRoot(userId)), relativePath);
    }

    public string GetGeneratedImageFullPath(string relativePath)
    {
        return Path.Combine(GetAiRendersOriginalsDir(ResolveStorageRoot()), relativePath);
    }

    public IReadOnlyList<string> GetGeneratedImageAssetFullPaths(string relativePath)
    {
        return
        [
            GetGeneratedImageFullPath(relativePath),
            Path.Combine(GetAiRendersDisplayDir(ResolveStorageRoot()), relativePath),
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

    private string BuildThumbnailPath(string storedFileName)
    {
        var fileName = Path.GetFileNameWithoutExtension(storedFileName);
        var extension = Path.GetExtension(storedFileName);
        return Path.Combine(GetAiRendersThumbnailsDir(ResolveStorageRoot()), $"{fileName}_thumb{extension}");
    }

    private string ResolveStorageRoot(Guid? explicitUserId = null)
    {
        if (explicitUserId.HasValue && explicitUserId.Value != Guid.Empty)
            return Path.Combine(_appFolder, "users", explicitUserId.Value.ToString("N"));

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
        Directory.CreateDirectory(GetAiProfileDir(storageRoot));
        Directory.CreateDirectory(GetAiRendersOriginalsDir(storageRoot));
        Directory.CreateDirectory(GetAiRendersDisplayDir(storageRoot));
        Directory.CreateDirectory(GetAiRendersThumbnailsDir(storageRoot));
    }

    private static string GetAiDir(string storageRoot) => Path.Combine(storageRoot, "ai");

    private static string GetAiProfileDir(string storageRoot) => Path.Combine(GetAiDir(storageRoot), "profile");

    private static string GetAiRendersDir(string storageRoot) => Path.Combine(GetAiDir(storageRoot), "renders");

    private static string GetAiRendersOriginalsDir(string storageRoot) => Path.Combine(GetAiRendersDir(storageRoot), "originals");

    private static string GetAiRendersDisplayDir(string storageRoot) => Path.Combine(GetAiRendersDir(storageRoot), "display");

    private static string GetAiRendersThumbnailsDir(string storageRoot) => Path.Combine(GetAiRendersDir(storageRoot), "thumbnails");

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
