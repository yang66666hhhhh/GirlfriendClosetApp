namespace ClosetApp.Application.Images;

public sealed record ImageAsset(
    string? StoredName,
    string? OriginalPath,
    string? DisplayPath,
    string? ThumbnailPath)
{
    public bool HasImage => !string.IsNullOrWhiteSpace(OriginalPath) ||
                            !string.IsNullOrWhiteSpace(DisplayPath);
}
