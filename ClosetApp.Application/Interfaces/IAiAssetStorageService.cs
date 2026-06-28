namespace ClosetApp.Application.Interfaces;

public interface IAiAssetStorageService
{
    Task<string> SaveProfileReferenceImageAsync(string sourcePath, string slotName, Guid? userId = null);
    Task<string> SaveGeneratedImageAsync(byte[] bytes, string mimeType);
    Task RestoreProfileReferenceImageAsync(string sourcePath, string storedFileName, Guid? userId = null);
    Task RestoreGeneratedImageAsync(string sourcePath, string storedFileName);
    Task TryDeleteProfileReferenceImageAsync(string? imagePath, Guid? userId = null);
    Task TryDeleteGeneratedImageAsync(string? imagePath);
    string GetProfileReferenceFullPath(string relativePath, Guid? userId = null);
    string GetGeneratedImageFullPath(string relativePath);
    IReadOnlyList<string> GetGeneratedImageAssetFullPaths(string relativePath);

    /// <summary>
    /// 返回当前用户的 AI 效果图显示目录（按 LocalUserId 隔离）。
    /// </summary>
    string GetAiRendersDisplayDirectory();

    /// <summary>
    /// 返回当前用户的 AI 效果图缩略图目录（按 LocalUserId 隔离）。
    /// </summary>
    string GetAiRendersThumbnailsDirectory();
}
