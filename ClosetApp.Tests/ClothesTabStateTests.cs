using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.States;
using Xunit;

namespace ClosetApp.Tests;

public class ClothesTabStateTests
{
    [Fact]
    public void ApplyFilter_WithCategoryAndSearch_ReturnsMatchingClothes()
    {
        var state = new ClothesTabState();
        state.SetClothes(
        [
            CreateClothing("Soft Knit", ClothingType.Top, GarmentType.Sweater, color: "Ivory"),
            CreateClothing("City Tote", ClothingType.Accessory, GarmentType.Bag, color: "Black"),
            CreateClothing("Weekend Tee", ClothingType.Top, GarmentType.TShirt, color: "Blue")
        ]);

        state.SetSelectedCategories([DisplayCategory.Topwear]);
        state.SetSearchText("ivory");

        var match = Assert.Single(state.FilteredClothes);
        Assert.Equal("Soft Knit", match.Name);
    }

    [Fact]
    public void ApplyFilter_SearchesTagNamesAndNotes()
    {
        var state = new ClothesTabState();
        state.SetClothes(
        [
            CreateClothing(
                "Date Dress",
                ClothingType.Dress,
                GarmentType.Dress,
                notes: "Dinner look",
                tags: [CreateTag("约会")]),
            CreateClothing("Office Shirt", ClothingType.Top, GarmentType.Shirt, notes: "Work staple")
        ]);

        state.SetSearchText("约会");

        var match = Assert.Single(state.FilteredClothes);
        Assert.Equal("Date Dress", match.Name);
    }

    [Fact]
    public void FilterSummary_WithSearchAndCategory_UsesCombinedSummary()
    {
        var state = new ClothesTabState();
        state.SetClothes([CreateClothing("Soft Knit", ClothingType.Top, GarmentType.Sweater)]);

        state.SetSelectedCategories([DisplayCategory.Topwear]);
        state.SetSearchText("soft");

        Assert.Equal("搜索「soft」+ 分类筛选", state.FilterSummary);
        Assert.True(state.HasActiveFilters);
    }

    private static Clothing CreateClothing(
        string name,
        ClothingType type,
        GarmentType garmentType,
        string? color = null,
        string? notes = null,
        IEnumerable<Tag>? tags = null)
    {
        var clothing = new Clothing
        {
            Name = name,
            Type = type,
            GarmentType = garmentType,
            Season = Season.AllSeason,
            Color = color,
            Notes = notes
        };

        foreach (var tag in tags ?? [])
        {
            clothing.ClothingTags.Add(new ClothingTag
            {
                ClothingId = clothing.Id,
                TagId = tag.Id,
                Tag = tag
            });
        }

        return clothing;
    }

    private static Tag CreateTag(string name)
    {
        return new Tag
        {
            Name = name,
            Color = "#FFFFFF"
        };
    }
}
