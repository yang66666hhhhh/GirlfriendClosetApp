using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Logic.States;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitPresentationTextTests
{
    [Fact]
    public void GetSortLabel_ReturnsChineseLabel()
    {
        Assert.Equal("评分", OutfitPresentationText.GetSortLabel(OutfitSortBy.Rating));
    }

    [Fact]
    public void BuildCompactWeatherCity_TrimsToTwoSegments()
    {
        Assert.Equal("Hangzhou · Xihu", OutfitPresentationText.BuildCompactWeatherCity("Hangzhou · Xihu · West Lake"));
    }

    [Fact]
    public void ResolveHeroSummaryText_PrefersSummaryText()
    {
        var recommendation = CreateRecommendation(
            reasonSummaryText: "更适合今天的温度。",
            primaryReason: "主因");

        Assert.Equal("更适合今天的温度。", OutfitPresentationText.ResolveHeroSummaryText(recommendation));
    }

    [Fact]
    public void BuildTodayHeroSupportText_WhenAlreadyWornToday_ReturnsRepeatMessage()
    {
        var recommendation = CreateRecommendation(isWornToday: true);

        Assert.Equal(
            "这套今天已经记过一次了；如果晚点还要出门，也可以继续穿它。",
            OutfitPresentationText.BuildTodayHeroSupportText(recommendation, hasTodayWornRecords: true, todayWornCount: 2));
    }

    [Fact]
    public void BuildTodayHeroSupportText_WhenHasTodayRecords_AppendsCountHint()
    {
        var recommendation = CreateRecommendation(primaryReason: "今天适合轻一点。");

        Assert.Equal(
            "今天适合轻一点。 今天已经记过 2 套，下一套可以换个感觉。",
            OutfitPresentationText.BuildTodayHeroSupportText(recommendation, hasTodayWornRecords: true, todayWornCount: 2));
    }

    private static RecommendedOutfitDto CreateRecommendation(
        bool isWornToday = false,
        string? reasonSummaryText = null,
        string? primaryReason = null)
    {
        var outfit = new Outfit
        {
            Id = Guid.NewGuid(),
            Name = "测试搭配",
            Scene = OutfitScene.Casual,
            Season = Season.Spring,
            WornDate = isWornToday ? DateTime.Today : null
        };

        return new RecommendedOutfitDto(
            outfit,
            95,
            primaryReason ?? "今天适合这套。",
            reasonSummaryText,
            primaryReason is null ? [] : [primaryReason]);
    }
}
