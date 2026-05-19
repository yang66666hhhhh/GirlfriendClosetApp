using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Clothing;
using Xunit;

namespace ClosetApp.Tests;

public class BatchClothingImportSummaryBuilderTests
{
    [Fact]
    public void Build_CollectsUnnamedAndMissingMetadataItems()
    {
        var request = new BatchClothingImportRequest(
        [
            new BatchClothingImportItem(@"C:\import\look-1.png", "未命名"),
            new BatchClothingImportItem(@"C:\import\look-2.png", "奶白外套"),
            new BatchClothingImportItem(@"C:\import\look-3.png", "黑色半裙")
        ],
            ClothingType.Unspecified,
            Season.Unspecified,
            null,
            null,
            null,
            0,
            []);

        var imported = new[]
        {
            new Clothing { Name = "未命名", Type = ClothingType.Unspecified, Season = Season.Unspecified },
            new Clothing { Name = "奶白外套", Type = ClothingType.Outerwear, Season = Season.Unspecified },
            new Clothing { Name = "黑色半裙", Type = ClothingType.Unspecified, Season = Season.Autumn }
        };

        var summary = BatchClothingImportSummaryBuilder.Build(request, imported);

        Assert.Equal(3, summary.ImportedCount);
        Assert.Single(summary.UnnamedItems);
        Assert.Equal("look-1.png", summary.UnnamedItems[0].SourceFileName);
        Assert.Equal(2, summary.UncategorizedItems.Count);
        Assert.Equal(2, summary.UnseasonedItems.Count);
    }

    [Fact]
    public void Build_WithCompleteItems_HasNoFollowUp()
    {
        var request = new BatchClothingImportRequest(
        [
            new BatchClothingImportItem(@"C:\import\coat.png", "奶白外套")
        ],
            ClothingType.Outerwear,
            Season.Winter,
            null,
            null,
            null,
            0,
            []);

        var imported = new[]
        {
            new Clothing { Name = "奶白外套", Type = ClothingType.Outerwear, Season = Season.Winter }
        };

        var summary = BatchClothingImportSummaryBuilder.Build(request, imported);

        Assert.False(summary.HasAnyFollowUp);
        Assert.Empty(summary.UnnamedItems);
        Assert.Empty(summary.UncategorizedItems);
        Assert.Empty(summary.UnseasonedItems);
    }
}
