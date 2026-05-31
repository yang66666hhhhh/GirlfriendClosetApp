namespace ClosetApp.Application.DTOs;

public sealed record WornRecordImageHealthDto(
    int RecordCount,
    int SnapshotClothingCount,
    int MissingImageCount,
    int RecordsWithMissingImages,
    IReadOnlyList<WornRecordMissingImageDto>? MissingRecords = null)
{
    public IReadOnlyList<WornRecordMissingImageDto> MissingRecordItems => MissingRecords ?? [];

    public bool HasMissingImages => MissingImageCount > 0;

    public string Summary => SnapshotClothingCount == 0
        ? "穿着历史里还没有可检查的快照单品。"
        : HasMissingImages
            ? $"{RecordsWithMissingImages} 条穿着记录里有 {MissingImageCount} 件快照单品缺图。"
            : $"已检查 {RecordCount} 条穿着记录、{SnapshotClothingCount} 件快照单品，历史图片都可用。";
}

public sealed record WornRecordMissingImageDto(
    Guid RecordId,
    DateTime WornDate,
    string OutfitName,
    int MissingImageCount)
{
    public string Summary => $"{WornDate:yyyy-MM-dd} · {OutfitName} · {MissingImageCount} 件缺图";
}
