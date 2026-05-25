using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.States;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitsTabStateTests
{
    [Fact]
    public void ApplyFilter_WithSearchSceneAndFavorite_ReturnsOnlyMatchingOutfits()
    {
        var favoriteMatch = CreateOutfit("豆沙通勤", OutfitScene.Work, Season.Autumn, isFavorite: true, notes: "奶咖上班");
        favoriteMatch.OutfitClothes.Add(new OutfitClothing
        {
            Clothing = new Clothing
            {
                Name = "奶油西装",
                Brand = "COS",
                Color = "米白"
            }
        });

        var sameSceneButNotFavorite = CreateOutfit("通勤备用", OutfitScene.Work, Season.Autumn, isFavorite: false);
        var favoriteButOtherScene = CreateOutfit("周末约会", OutfitScene.Date, Season.Autumn, isFavorite: true);

        var state = new OutfitsTabState();
        state.SetOutfits([favoriteMatch, sameSceneButNotFavorite, favoriteButOtherScene]);
        state.SetSearchText("奶油");
        state.SetSelectedScene(OutfitScene.Work);
        state.SetFavoriteOnly(true);

        var result = Assert.Single(state.Outfits);
        Assert.Equal("豆沙通勤", result.Name);
        Assert.Equal("搜索「奶油」 + 通勤 + 仅收藏", state.FilterSummary);
    }

    [Fact]
    public void ApplyFilter_WithSeason_IncludesAllSeasonOutfits()
    {
        var spring = CreateOutfit("春日轻搭", OutfitScene.Casual, Season.Spring);
        var allSeason = CreateOutfit("四季通勤", OutfitScene.Work, Season.AllSeason);
        var winter = CreateOutfit("冬日大衣", OutfitScene.Work, Season.Winter);

        var state = new OutfitsTabState();
        state.SetOutfits([spring, allSeason, winter]);
        state.SetSelectedSeason(Season.Spring);

        Assert.Equal(2, state.Outfits.Count);
        Assert.Contains(state.Outfits, outfit => outfit.Name == "春日轻搭");
        Assert.Contains(state.Outfits, outfit => outfit.Name == "四季通勤");
    }

    [Fact]
    public void SetOutfits_WithoutFilters_KeepsAllOutfitsVisible()
    {
        var state = new OutfitsTabState();
        state.BeginLoad();

        state.SetOutfits(
        [
            CreateOutfit("工作日", OutfitScene.Work, Season.Autumn),
            CreateOutfit("周末", OutfitScene.Casual, Season.AllSeason)
        ]);

        Assert.False(state.IsLoading);
        Assert.False(state.IsEmpty);
        Assert.False(state.IsFilteredEmpty);
        Assert.Equal(2, state.TotalCount);
        Assert.Equal(2, state.OutfitCount);
        Assert.Equal("全部搭配", state.FilterSummary);
    }

    [Fact]
    public void ApplyFilter_WithSearchSceneSeasonFavoriteAndSort_ReturnsSortedMatches()
    {
        var first = CreateOutfit("周一通勤", OutfitScene.Work, Season.Autumn, isFavorite: true, notes: "羊毛西装");
        first.WearCount = 6;
        first.OutfitClothes.Add(new OutfitClothing
        {
            Clothing = new Clothing
            {
                Name = "羊毛大衣",
                Brand = "COS",
                Color = "驼色"
            }
        });

        var second = CreateOutfit("轻松通勤", OutfitScene.Work, Season.AllSeason, isFavorite: true, notes: "羊毛针织");
        second.WearCount = 3;
        second.OutfitClothes.Add(new OutfitClothing
        {
            Clothing = new Clothing
            {
                Name = "羊毛开衫",
                Brand = "Uniqlo",
                Color = "灰色"
            }
        });

        var excluded = CreateOutfit("周末约会", OutfitScene.Date, Season.Autumn, isFavorite: true, notes: "羊毛裙");

        var state = new OutfitsTabState();
        state.SetOutfits([second, excluded, first]);
        state.SetSearchText("羊毛");
        state.SetSelectedScene(OutfitScene.Work);
        state.SetSelectedSeason(Season.Autumn);
        state.SetFavoriteOnly(true);
        state.SetSortBy(OutfitSortBy.WearCount);

        Assert.True(new[] { "周一通勤", "轻松通勤" }.SequenceEqual(state.Outfits.Select(outfit => outfit.Name)));
        Assert.Equal("搜索「羊毛」 + 通勤 + 秋季 + 仅收藏", state.FilterSummary);
        Assert.Equal(2, state.OutfitCount);
    }

    [Fact]
    public void ToggleHistoryExpanded_UpdatesToggleText()
    {
        var state = new OutfitsTabState();

        Assert.False(state.IsHistoryExpanded);
        Assert.Equal("查看记录日历", state.HistoryToggleText);

        state.ToggleHistoryExpanded();

        Assert.True(state.IsHistoryExpanded);
        Assert.Equal("收起记录日历", state.HistoryToggleText);
    }

    private static Outfit CreateOutfit(
        string name,
        OutfitScene scene,
        Season season,
        bool isFavorite = false,
        string? notes = null)
    {
        var outfit = new Outfit
        {
            Name = name,
            Scene = scene,
            Season = season,
            Notes = notes
        };

        if (isFavorite)
            outfit.Favorites.Add(new Favorite { OutfitId = outfit.Id });

        return outfit;
    }
}
