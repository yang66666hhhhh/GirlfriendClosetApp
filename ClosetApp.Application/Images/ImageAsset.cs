namespace ClosetApp.Application.Images;

public sealed record ImageAsset(
    string? StoredName,
    string? DisplayPath,
    string? ThumbnailPath)
{
    public bool HasImage => !string.IsNullOrWhiteSpace(DisplayPath);
}
