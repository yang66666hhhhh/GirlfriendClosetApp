namespace ClosetApp.Application.DTOs;

public sealed record WornRecordImageHealthDto(
    int RecordCount,
    int SnapshotClothingCount,
    int MissingImageCount,
    int RecordsWithMissingImages)
{
    public bool HasMissingImages => MissingImageCount > 0;

    public string Summary => SnapshotClothingCount == 0
        ? "穿着历史里还没有可检查的快照单品。"
        : HasMissingImages
            ? $"{RecordsWithMissingImages} 条穿着记录里有 {MissingImageCount} 件快照单品缺图。"
            : $"已检查 {RecordCount} 条穿着记录、{SnapshotClothingCount} 件快照单品，历史图片都可用。";
}
