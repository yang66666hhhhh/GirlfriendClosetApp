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

        state.SetSelectedType(ClothingType.Top);
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

        state.SetSelectedType(ClothingType.Top);
        state.SetSearchText("soft");

        Assert.Equal("搜索「soft」 + 分类", state.FilterSummary);
        Assert.True(state.HasActiveFilters);
    }

    [Fact]
    public void ApplyFilter_WithSeasonTagAndFavorite_ReturnsOnlyMatchingClothes()
    {
        var tag = CreateTag("通勤");
        var matching = CreateClothing(
            "Office Knit",
            ClothingType.Top,
            GarmentType.Sweater,
            tags: [tag]);
        matching.Season = Season.Autumn;
        matching.IsFavorite = true;
        matching.FavoriteLevel = 4;

        var otherSeason = CreateClothing("Summer Tee", ClothingType.Top, GarmentType.TShirt, tags: [tag]);
        otherSeason.Season = Season.Summer;
        otherSeason.IsFavorite = true;
        otherSeason.FavoriteLevel = 4;

        var notFavorite = CreateClothing("Commute Shirt", ClothingType.Top, GarmentType.Shirt, tags: [tag]);
        notFavorite.Season = Season.Autumn;
        notFavorite.IsFavorite = false;
        notFavorite.FavoriteLevel = 2;

        var state = new ClothesTabState();
        state.SetClothes([matching, otherSeason, notFavorite]);
        state.SetSelectedSeason(Season.Autumn);
        state.SetSelectedTagIds([tag.Id]);
        state.SetFavoriteOnly(true);

        var result = Assert.Single(state.FilteredClothes);
        Assert.Equal("Office Knit", result.Name);
    }

    [Fact]
    public void ApplyFilter_WithSingleType_KeepsOnlyThatClothingType()
    {
        var state = new ClothesTabState();
        state.SetClothes(
        [
            CreateClothing("Weekend Knit", ClothingType.Top, GarmentType.Sweater),
            CreateClothing("Daily Pants", ClothingType.Bottom, GarmentType.Trousers),
            CreateClothing("Soft Heels", ClothingType.Shoes, GarmentType.Heels)
        ]);

        state.SetSelectedType(ClothingType.Bottom);

        var result = Assert.Single(state.FilteredClothes);
        Assert.Equal("Daily Pants", result.Name);
        Assert.Equal(ClothingType.Bottom, state.SelectedType);
    }

    [Fact]
    public void ApplyFilter_WithType_DoesNotMixNearbyCategories()
    {
        var state = new ClothesTabState();
        state.SetClothes(
        [
            CreateClothing("Weekend Knit", ClothingType.Top, GarmentType.Sweater),
            CreateClothing("Wool Coat", ClothingType.Outerwear, GarmentType.Coat),
            CreateClothing("Daily Pants", ClothingType.Bottom, GarmentType.Trousers),
            CreateClothing("Pleated Skirt", ClothingType.Skirt, GarmentType.Skirt)
        ]);

        state.SetSelectedType(ClothingType.Top);
        Assert.Equal("Weekend Knit", Assert.Single(state.FilteredClothes).Name);

        state.SetSelectedType(ClothingType.Bottom);
        Assert.Equal("Daily Pants", Assert.Single(state.FilteredClothes).Name);
    }

    [Fact]
    public void ApplyFilter_WithType_CanFindUnspecifiedItems()
    {
        var state = new ClothesTabState();
        state.SetClothes(
        [
            CreateClothing("Ready To Sort", ClothingType.Unspecified, null),
            CreateClothing("Weekend Knit", ClothingType.Top, GarmentType.Sweater)
        ]);

        state.SetSelectedType(ClothingType.Unspecified);

        var result = Assert.Single(state.FilteredClothes);
        Assert.Equal("Ready To Sort", result.Name);
    }

    private static Clothing CreateClothing(
        string name,
        ClothingType type,
        GarmentType? garmentType,
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
