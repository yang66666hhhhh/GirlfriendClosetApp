namespace ClosetApp.Application.Interfaces;

public interface IAiAssetStorageService
{
    Task<string> SaveProfileReferenceImageAsync(string sourcePath, string slotName);
    Task<string> SaveGeneratedImageAsync(byte[] bytes, string mimeType);
    Task RestoreProfileReferenceImageAsync(string sourcePath, string storedFileName);
    Task RestoreGeneratedImageAsync(string sourcePath, string storedFileName);
    Task TryDeleteProfileReferenceImageAsync(string? imagePath);
    Task TryDeleteGeneratedImageAsync(string? imagePath);
    string GetProfileReferenceFullPath(string relativePath);
    string GetGeneratedImageFullPath(string relativePath);
    IReadOnlyList<string> GetGeneratedImageAssetFullPaths(string relativePath);
}
