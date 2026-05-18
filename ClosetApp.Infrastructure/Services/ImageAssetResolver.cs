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
            return new ImageAsset(null, null, null, null);

        if (Path.IsPathRooted(imagePath) && File.Exists(imagePath))
            return new ImageAsset(imagePath, imagePath, imagePath, null);

        var appRelativePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
        if (File.Exists(appRelativePath))
            return new ImageAsset(imagePath, appRelativePath, appRelativePath, null);

        var originalPath = _imageStorage.GetImageFullPath(imagePath);
        var displayPath = _imageStorage.GetDisplayFullPath(imagePath);
        var thumbnailPath = _imageStorage.GetThumbnailFullPath(imagePath);

        return new ImageAsset(
            imagePath,
            File.Exists(originalPath) ? originalPath : null,
            File.Exists(displayPath) ? displayPath : null,
            File.Exists(thumbnailPath) ? thumbnailPath : null);
    }

    public string? ResolvePath(string? imagePath, ImageVariant variant)
    {
        var asset = Resolve(imagePath);
        return variant switch
        {
            ImageVariant.Original => asset.OriginalPath ?? asset.DisplayPath,
            ImageVariant.Display => asset.DisplayPath ?? asset.OriginalPath,
            ImageVariant.Thumbnail => asset.ThumbnailPath ?? asset.DisplayPath ?? asset.OriginalPath,
            _ => asset.DisplayPath ?? asset.OriginalPath
        };
    }
}
