namespace ClosetApp.Application.Interfaces;

public interface IImageStorageService
{
    Task<string> SaveImageAsync(string sourcePath);
    Task<string> SaveThumbnailAsync(string sourcePath, int maxSize = 200);
    Task<bool> EnsureThumbnailAsync(string imagePath, int maxSize = 200);
    Task<bool> EnsureDisplayAsync(string imagePath, int maxWidth = 900);
    Task RestoreImageAsync(string sourcePath, string storedFileName);
    Task DeleteImageAsync(string imagePath);
    Task DeleteImageWithThumbnailAsync(string imagePath);

    /// <summary>
    /// 尝试删除图片及其缩略图，忽略空路径和删除异常。
    /// </summary>
    Task TryDeleteImageAsync(string? imagePath);

    string GetImageFullPath(string relativePath);
    string GetDisplayFullPath(string relativePath);
    string GetThumbnailFullPath(string relativePath);
    IReadOnlyList<string> GetOriginalImageFullPaths();
    IReadOnlyList<string> GetImageAssetFullPaths(string relativePath);
}
