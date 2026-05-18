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

public sealed record OrphanOriginalsResult(
    int OrphanCount,
    long TotalBytes)
{
    public bool HasOrphans => OrphanCount > 0;
}

public sealed record OrphanOriginalsCleanupResult(
    int DeletedOriginalCount,
    int DeletedDerivedAssetCount,
    long FreedBytes)
{
    public string Summary =>
        DeletedOriginalCount == 0
            ? "没有发现可清理的孤儿原图。"
            : $"已清理 {DeletedOriginalCount} 张孤儿原图和 {DeletedDerivedAssetCount} 个派生缓存，释放 {FormatSize(FreedBytes)}。";

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
    }
}
