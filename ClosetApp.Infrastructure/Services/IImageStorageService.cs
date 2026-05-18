namespace ClosetApp.Infrastructure.Services;

public interface IImageStorageService
{
    Task<string> SaveImageAsync(string sourcePath);
    Task<string> SaveThumbnailAsync(string sourcePath, int maxSize = 200);
    Task<bool> EnsureThumbnailAsync(string imagePath, int maxSize = 200);
    Task<bool> EnsureDisplayAsync(string imagePath, int maxWidth = 900);
    Task RestoreImageAsync(string sourcePath, string storedFileName);
    Task DeleteImageAsync(string imagePath);
    Task DeleteImageWithThumbnailAsync(string imagePath);
    string GetImageFullPath(string relativePath);
    string GetDisplayFullPath(string relativePath);
    string GetThumbnailFullPath(string relativePath);
}
