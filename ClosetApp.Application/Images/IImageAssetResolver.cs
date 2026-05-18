namespace ClosetApp.Application.Images;

public interface IImageAssetResolver
{
    ImageAsset Resolve(string? imagePath);
    string? ResolvePath(string? imagePath, ImageVariant variant);
}
