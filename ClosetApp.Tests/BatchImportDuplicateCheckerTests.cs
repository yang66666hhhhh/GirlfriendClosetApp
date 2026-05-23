using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Clothing;
using Xunit;

namespace ClosetApp.Tests;

public class BatchImportDuplicateCheckerTests
{
    [Fact]
    public void Analyze_DetectsDuplicatesInsideSelection_AndAgainstExistingClothes()
    {
        var previewItems = new[]
        {
            new BatchClothingImportPreviewItem(@"C:\import\IMG_1001.jpg", "IMG_1001.jpg", "未命名"),
            new BatchClothingImportPreviewItem(@"C:\import\IMG_1001-copy.jpg", "IMG_1001.jpg", "未命名"),
            new BatchClothingImportPreviewItem(@"C:\import\IMG_2002.jpg", "IMG_2002.jpg", "未命名")
        };
        var existing = new[]
        {
            new Clothing { ImagePath = "IMG_2002.jpg" },
            new Clothing { ImagePath = "other.jpg" }
        };

        var metadata = new Dictionary<string, (long Length, int Width, int Height)>
        {
            [@"C:\import\IMG_1001.jpg"] = (1024, 1080, 1440),
            [@"C:\import\IMG_1001-copy.jpg"] = (1024, 1080, 1440),
            [@"C:\import\IMG_2002.jpg"] = (2048, 1200, 1600),
            ["IMG_2002.jpg"] = (2048, 1200, 1600),
            ["other.jpg"] = (999, 900, 1200)
        };

        var result = BatchImportDuplicateChecker.Analyze(
            previewItems,
            existing,
            path => metadata.TryGetValue(path, out var value) ? value : null);

        Assert.True(result.HasDuplicateFileNameInSelection);
        Assert.True(result.HasDuplicateSignatureInSelection);
        Assert.True(result.HasExistingFileNameMatch);
        Assert.True(result.HasExistingSignatureMatch);
        Assert.True(result.HasAnyDuplicateRisk);
        Assert.Equal(2, result.RiskItemCount);
        Assert.Contains(@"C:\import\IMG_1001-copy.jpg", result.RiskFilePaths);
        Assert.Contains(@"C:\import\IMG_2002.jpg", result.RiskFilePaths);
        Assert.Equal("本批里有同文件名；本批里有同尺寸/大小", result.GetRiskReason(@"C:\import\IMG_1001-copy.jpg"));
        Assert.Equal("衣柜已有同文件名；衣柜已有同尺寸/大小", result.GetRiskReason(@"C:\import\IMG_2002.jpg"));
    }

    [Fact]
    public void Analyze_WithDistinctFiles_ReturnsNoRisk()
    {
        var previewItems = new[]
        {
            new BatchClothingImportPreviewItem(@"C:\import\a.jpg", "a.jpg", "未命名"),
            new BatchClothingImportPreviewItem(@"C:\import\b.jpg", "b.jpg", "未命名")
        };
        var existing = new[]
        {
            new Clothing { ImagePath = "c.jpg" }
        };

        var metadata = new Dictionary<string, (long Length, int Width, int Height)>
        {
            [@"C:\import\a.jpg"] = (101, 1000, 1200),
            [@"C:\import\b.jpg"] = (202, 900, 1200),
            ["c.jpg"] = (303, 1500, 1800)
        };

        var result = BatchImportDuplicateChecker.Analyze(
            previewItems,
            existing,
            path => metadata.TryGetValue(path, out var value) ? value : null);

        Assert.False(result.HasAnyDuplicateRisk);
        Assert.Empty(result.RiskFilePaths);
        Assert.Empty(result.RiskReasons);
    }
}
