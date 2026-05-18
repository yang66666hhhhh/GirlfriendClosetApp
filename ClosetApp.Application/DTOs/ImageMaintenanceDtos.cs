namespace ClosetApp.Application.DTOs;

public sealed record ThumbnailRebuildResult(
    int ScannedImageCount,
    int MissingThumbnailCount,
    int RebuiltCount,
    int SkippedCount,
    int MissingSourceCount)
{
    public string Summary =>
        MissingThumbnailCount == 0
            ? $"已检查 {ScannedImageCount} 张图片，图片缓存完整。"
            : $"已检查 {ScannedImageCount} 张图片，重建 {RebuiltCount} 组图片缓存，已有缓存 {SkippedCount} 组，原图缺失 {MissingSourceCount} 张。";
}
