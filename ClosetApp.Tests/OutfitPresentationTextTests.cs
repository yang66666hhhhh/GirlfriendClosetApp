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
    public void BuildCompactWeatherSummary_ReturnsTemperatureAndCondition()
    {
        Assert.Equal("18°C · 多云", OutfitPresentationText.BuildCompactWeatherSummary(18, "多云"));
    }

    [Fact]
    public void BuildRecommendationCountText_WhenEmpty_ReturnsFallback()
    {
        Assert.Equal("暂无", OutfitPresentationText.BuildRecommendationCountText([]));
    }

    [Fact]
    public void CountTodayWornRecords_ReturnsOnlyTodayItems()
    {
        var items = new[]
        {
            CreateRecentWornListItem(DateTime.Today),
            CreateRecentWornListItem(DateTime.Today.AddDays(-1)),
            CreateRecentWornListItem(DateTime.Today)
        };

        Assert.Equal(2, OutfitPresentationText.CountTodayWornRecords(items));
    }

    [Fact]
    public void BuildTodayWornStatusText_WhenEmpty_ReturnsFallback()
    {
        Assert.Equal("今天还没记录", OutfitPresentationText.BuildTodayWornStatusText(0));
    }

    [Fact]
    public void BuildHistoryQuickText_WhenEmpty_ReturnsFallback()
    {
        Assert.Equal("暂无记录", OutfitPresentationText.BuildHistoryQuickText(0));
    }

    [Fact]
    public void BuildHistorySummaryText_WhenHasRecords_ReturnsGuidance()
    {
        Assert.Contains("最近 2 条穿着记录", OutfitPresentationText.BuildHistorySummaryText(2));
    }

    [Fact]
    public void BuildDefaultCalendarSummaryText_ReturnsHint()
    {
        Assert.Contains("按月份回看每天穿了哪套", OutfitPresentationText.BuildDefaultCalendarSummaryText());
    }

    [Fact]
    public void BuildCalendarSummaryText_WhenHasRecords_ReturnsMonthlySummary()
    {
        var outfit = new Outfit { Name = "常穿搭配" };
        var records = new[]
        {
            new OutfitWornRecord { WornDate = DateTime.Today, Outfit = outfit },
            new OutfitWornRecord { WornDate = DateTime.Today.AddDays(-1), Outfit = outfit }
        };

        var summary = OutfitPresentationText.BuildCalendarSummaryText(records);

        Assert.Contains("本月 2 次记录", summary);
        Assert.Contains("2 天有穿搭", summary);
        Assert.Contains("最常穿「常穿搭配」", summary);
    }

    [Fact]
    public void BuildRecommendationReadinessCountText_WhenHasMatchingSeason_UsesRatio()
    {
        var readiness = new RecommendationReadinessSummaryDto(
            "title",
            "detail",
            Season.Autumn,
            3,
            5);

        Assert.Equal("3/5 套对季", OutfitPresentationText.BuildRecommendationReadinessCountText(readiness));
    }

    [Fact]
    public void BuildRecommendationMissingSeasonText_WhenMissingSeasonExists_ReturnsSuggestion()
    {
        var readiness = new RecommendationReadinessSummaryDto(
            "title",
            "detail",
            Season.Winter,
            0,
            5);

        Assert.Equal("建议补 冬季 搭配", OutfitPresentationText.BuildRecommendationMissingSeasonText(readiness, hasRecommendationGap: true));
    }

    [Fact]
    public void BuildTodayHeroPrimaryActionText_WhenHasTodayRecordsAndNotWornToday_ReturnsRetryCopy()
    {
        var recommendation = CreateRecommendation(primaryReason: "今天适合轻一点。");

        Assert.Equal("再记这套", OutfitPresentationText.BuildTodayHeroPrimaryActionText(recommendation, hasTodayWornRecords: true));
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

    [Fact]
    public void RecommendedOutfitDto_UserReasonHeadline_WhenTemperatureReason_ReturnsWeatherCopy()
    {
        var recommendation = CreateRecommendation(primaryReason: "温度在 22°C 左右，这套的季节感正合适。");

        Assert.Equal("今天的天气条件和这套搭配比较合拍。", recommendation.UserReasonHeadline);
    }

    [Fact]
    public void RecommendedOutfitDto_HighlightTags_WhenFavoriteAndColorReasons_ReturnsReadableTags()
    {
        var recommendation = CreateRecommendation(
            primaryReason: "这套被你标记过收藏，值得优先翻出来穿。",
            reasons:
            [
                "这套被你标记过收藏，值得优先翻出来穿。",
                "颜色也接近你常选的那一类。",
                "风格标签也比较贴近你的常用偏好。"
            ]);

        Assert.Contains("你的偏爱", recommendation.HighlightTags);
        Assert.Contains("颜色顺手", recommendation.HighlightTags);
    }

    [Fact]
    public void RecommendedOutfitDto_CautionText_WhenAlreadyWornToday_ReturnsRepeatWarning()
    {
        var recommendation = CreateRecommendation(isWornToday: true);

        Assert.Equal("今天已经记录过这套，除非你想重复穿。", recommendation.CautionText);
    }

    private static RecommendedOutfitDto CreateRecommendation(
        bool isWornToday = false,
        string? reasonSummaryText = null,
        string? primaryReason = null,
        IReadOnlyList<string>? reasons = null)
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
            reasons ?? (primaryReason is null ? [] : [primaryReason]));
    }

    private static RecentWornListItem CreateRecentWornListItem(DateTime wornDate)
    {
        return new RecentWornListItem(
            Guid.NewGuid(),
            wornDate,
            "date",
            "搭配",
            "10:00",
            "meta",
            [],
            "summary",
            "note",
            "sync");
    }
}
