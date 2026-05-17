using System.IO;

namespace ClosetApp.Application.DTOs;

public sealed record BackupValidationResult(
    string Format,
    int ClothingCount,
    int OutfitCount,
    int TagCount,
    int WornRecordCount,
    int FavoriteCount,
    int ReferencedImageCount,
    int IncludedImageCount,
    int MissingImageCount,
    IReadOnlyList<string> Warnings)
{
    public bool IsEmptyBackup =>
        ClothingCount == 0 &&
        OutfitCount == 0 &&
        TagCount == 0 &&
        WornRecordCount == 0 &&
        FavoriteCount == 0;

    public bool HasWarnings => Warnings.Count > 0;
}

public sealed record BackupExportResult(
    string FilePath,
    string Format,
    DateTime ExportedAt,
    long FileSizeBytes,
    int ClothingCount,
    int OutfitCount,
    int TagCount,
    int WornRecordCount,
    int FavoriteCount,
    int IncludedImageCount,
    int MissingImageCount,
    IReadOnlyList<string> Warnings)
{
    public string Summary =>
        Format == "zip"
            ? $"导出 {ClothingCount} 件衣服、{OutfitCount} 套搭配、{TagCount} 个标签，打包 {IncludedImageCount} 张图片。"
            : $"导出 {ClothingCount} 件衣服、{OutfitCount} 套搭配、{TagCount} 个标签的核心数据。";
}

public sealed record BackupImportResult(
    string FilePath,
    string Format,
    DateTime ImportedAt,
    int ClothingCount,
    int OutfitCount,
    int TagCount,
    int WornRecordCount,
    int FavoriteCount,
    int RestoredImageCount,
    int MissingImageCount,
    IReadOnlyList<string> MissingImageFiles,
    IReadOnlyList<string> Warnings)
{
    public string Summary =>
        Format == "zip"
            ? $"导入 {ClothingCount} 件衣服、{OutfitCount} 套搭配、{TagCount} 个标签，恢复 {RestoredImageCount} 张图片。"
            : $"导入 {ClothingCount} 件衣服、{OutfitCount} 套搭配、{TagCount} 个标签的核心数据。";

    public bool ShouldSuggestRepair => MissingImageCount > 0 || (Format == "json" && MissingImageFiles.Count > 0);
}

public sealed record BackupHistoryItem(
    DateTime Timestamp,
    string Operation,
    string Format,
    string FilePath,
    long FileSizeBytes,
    bool Success,
    string Summary,
    string? ErrorMessage = null)
{
    public string FileName => Path.GetFileName(FilePath);

    public string TimestampText => Timestamp.ToString("yyyy-MM-dd HH:mm");

    public string StatusText => Success ? "成功" : "失败";

    public string OperationText => Operation == "Import" ? "导入" : "导出";

    public string MetaText
    {
        get
        {
            var parts = new List<string>
            {
                OperationText,
                Format.ToUpperInvariant()
            };

            if (FileSizeBytes > 0)
                parts.Add(FormatSize(FileSizeBytes));

            return string.Join(" · ", parts);
        }
    }

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
