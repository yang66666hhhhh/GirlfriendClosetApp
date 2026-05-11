namespace ClosetApp.Application.Images;

public interface IImageAssetResolver
{
    ImageAsset Resolve(string? imagePath);
}
