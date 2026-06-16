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

    /// <summary>
    /// 返回当前用户的原图目录（按 LocalUserId 隔离）。
    /// </summary>
    string GetOriginalsDirectory();

    /// <summary>
    /// 返回当前用户的主视觉缓存目录（按 LocalUserId 隔离）。
    /// </summary>
    string GetDisplayDirectory();

    /// <summary>
    /// 返回当前用户的小预览缓存目录（按 LocalUserId 隔离）。
    /// </summary>
    string GetThumbnailsDirectory();

    /// <summary>
    /// 一次性迁移：将全局图片目录中的文件复制到当前用户的隔离目录。
    /// 仅在用户目录为空且全局目录有文件时执行复制。
    /// </summary>
    Task MigrateGlobalImagesAsync();
}
