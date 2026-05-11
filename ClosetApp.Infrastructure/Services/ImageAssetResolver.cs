using ClosetApp.Application.Images;

namespace ClosetApp.Infrastructure.Services;

public sealed class ImageAssetResolver : IImageAssetResolver
{
    private readonly IImageStorageService _imageStorage;

    public ImageAssetResolver(IImageStorageService imageStorage)
    {
        _imageStorage = imageStorage;
    }

    public ImageAsset Resolve(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return new ImageAsset(null, null, null);

        if (Path.IsPathRooted(imagePath) && File.Exists(imagePath))
            return new ImageAsset(imagePath, imagePath, null);

        var appRelativePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
        if (File.Exists(appRelativePath))
            return new ImageAsset(imagePath, appRelativePath, null);

        var storedPath = _imageStorage.GetImageFullPath(imagePath);
        var thumbnailPath = _imageStorage.GetThumbnailFullPath(imagePath);

        return new ImageAsset(
            imagePath,
            File.Exists(storedPath) ? storedPath : null,
            File.Exists(thumbnailPath) ? thumbnailPath : null);
    }
}
