using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using Xunit;

namespace ClosetApp.Tests;

public class RecommendationReadinessSummaryTests
{
    [Fact]
    public void Build_WithNoOutfits_ReturnsStarterGuidance()
    {
        var summary = GetRecommendationReadinessSummary.Build([], 22);

        Assert.Equal("还没有搭配", summary.Title);
        Assert.True(summary.HasGap);
        Assert.Equal(0, summary.ReadyOutfitCount);
    }

    [Fact]
    public void Build_WithOutfitsWithoutClothes_ReturnsCompletionGuidance()
    {
        var summary = GetRecommendationReadinessSummary.Build(
            [Outfit("Draft", Season.Spring, withClothes: false)],
            22);

        Assert.Equal("搭配还缺衣物", summary.Title);
        Assert.True(summary.HasGap);
    }

    [Fact]
    public void Build_WithOnlyUnspecifiedSeasons_ReturnsSeasonGuidance()
    {
        var summary = GetRecommendationReadinessSummary.Build(
            [Outfit("Draft", Season.Unspecified)],
            22);

        Assert.Equal("搭配还没补季节", summary.Title);
        Assert.True(summary.HasGap);
    }

    [Fact]
    public void Build_WithMissingCurrentSeason_ReturnsMissingSeason()
    {
        var summary = GetRecommendationReadinessSummary.Build(
            [Outfit("Winter Coat", Season.Winter)],
            29);

        Assert.Equal("缺少夏季搭配", summary.Title);
        Assert.Equal(Season.Summer, summary.MissingSeason);
        Assert.True(summary.HasGap);
    }

    [Fact]
    public void Build_WithMatchingSeason_ReturnsReadySummary()
    {
        var summary = GetRecommendationReadinessSummary.Build(
            [
                Outfit("Spring Look", Season.Spring),
                Outfit("All Season", Season.AllSeason)
            ],
            20);

        Assert.Equal("推荐准备好了", summary.Title);
        Assert.False(summary.HasGap);
        Assert.Equal(2, summary.MatchingSeasonCount);
    }

    private static Outfit Outfit(string name, Season season, bool withClothes = true)
    {
        var outfit = new Outfit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Scene = OutfitScene.Casual,
            Season = season
        };

        if (withClothes)
        {
            outfit.OutfitClothes.Add(new OutfitClothing
            {
                OutfitId = outfit.Id,
                ClothingId = Guid.NewGuid()
            });
        }

        return outfit;
    }
}
