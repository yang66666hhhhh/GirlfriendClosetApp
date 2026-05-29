using System.IO;
using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Logic.Components.Clothing;

public static class BatchClothingImportSummaryBuilder
{
    public static BatchClothingImportSummary Build(
        BatchClothingImportRequest request,
        IReadOnlyList<global::ClosetApp.Domain.Entities.Clothing> importedClothes)
    {
        // Keep request/result paired by index so the summary can point back to the original source file.
        var items = importedClothes
            .Select((clothing, index) => CreateItem(clothing, request.Items.ElementAtOrDefault(index)))
            .ToList();

        return new BatchClothingImportSummary(
            items.Count,
            items.Where(item => item.IsUnnamed).ToList(),
            items.Where(item => item.IsUncategorized).ToList(),
            items.Where(item => item.IsUnseasoned).ToList());
    }

    private static BatchClothingImportSummaryItem CreateItem(
        global::ClosetApp.Domain.Entities.Clothing clothing,
        BatchClothingImportItem? requestItem)
    {
        var sourceFileName = string.IsNullOrWhiteSpace(requestItem?.SourceImagePath)
            ? "原图文件"
            : Path.GetFileName(requestItem.SourceImagePath);

        return new BatchClothingImportSummaryItem(
            string.IsNullOrWhiteSpace(clothing.Name) ? BatchClothingImportBuilder.DefaultName : clothing.Name,
            sourceFileName,
            string.IsNullOrWhiteSpace(clothing.Name) || clothing.Name == BatchClothingImportBuilder.DefaultName,
            clothing.Type == ClothingType.Unspecified,
            clothing.Season == Season.Unspecified);
    }
}

public sealed record BatchClothingImportSummary(
    int ImportedCount,
    IReadOnlyList<BatchClothingImportSummaryItem> UnnamedItems,
    IReadOnlyList<BatchClothingImportSummaryItem> UncategorizedItems,
    IReadOnlyList<BatchClothingImportSummaryItem> UnseasonedItems)
{
    public bool HasUnnamedItems => UnnamedItems.Count > 0;
    public bool HasUncategorizedItems => UncategorizedItems.Count > 0;
    public bool HasUnseasonedItems => UnseasonedItems.Count > 0;
    public bool HasAnyFollowUp => HasUnnamedItems || HasUncategorizedItems || HasUnseasonedItems;
}

public sealed record BatchClothingImportSummaryItem(
    string DisplayName,
    string SourceFileName,
    bool IsUnnamed,
    bool IsUncategorized,
    bool IsUnseasoned);
