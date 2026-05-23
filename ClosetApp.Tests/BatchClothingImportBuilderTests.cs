using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Clothing;
using Xunit;

namespace ClosetApp.Tests;

public class BatchClothingImportBuilderTests
{
    [Fact]
    public void CreateClothing_UsesDefaultNameAndSharedOptions()
    {
        var tag = new Tag { Id = Guid.NewGuid(), Name = "通勤", Color = "#FFFFFF" };
        var options = new BatchClothingImportOptions(
            ClothingType.Outerwear,
            Season.Winter,
            " 奶白 ",
            "  Uniqlo ",
            "  一批同类外套 ",
            4,
            [tag]);

        var clothing = BatchClothingImportBuilder.CreateClothing("stored.png", options);

        Assert.Equal("未命名", clothing.Name);
        Assert.Equal(ClothingType.Outerwear, clothing.Type);
        Assert.Equal(Season.Winter, clothing.Season);
        Assert.Equal("stored.png", clothing.ImagePath);
        Assert.Equal("奶白", clothing.Color);
        Assert.Equal("Uniqlo", clothing.Brand);
        Assert.Equal("一批同类外套", clothing.Notes);
        Assert.Equal(4, clothing.FavoriteLevel);

        var clothingTag = Assert.Single(clothing.ClothingTags);
        Assert.Equal(tag.Id, clothingTag.TagId);
    }

    [Fact]
    public void CreateClothing_WithBlankOptions_KeepsFieldsReadyForManualEditing()
    {
        var options = new BatchClothingImportOptions(
            ClothingType.Unspecified,
            Season.Unspecified,
            "",
            " ",
            null,
            0,
            []);

        var clothing = BatchClothingImportBuilder.CreateClothing("stored.png", options);

        Assert.Equal("未命名", clothing.Name);
        Assert.Equal(ClothingType.Unspecified, clothing.Type);
        Assert.Equal(Season.Unspecified, clothing.Season);
        Assert.Null(clothing.Color);
        Assert.Null(clothing.Brand);
        Assert.Null(clothing.Notes);
        Assert.Equal(0, clothing.FavoriteLevel);
        Assert.Empty(clothing.ClothingTags);
    }
}
