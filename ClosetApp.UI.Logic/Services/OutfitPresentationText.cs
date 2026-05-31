using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.States;

namespace ClosetApp.UI.Logic.Services;

public static class OutfitPresentationText
{
    public static string GetSortLabel(OutfitSortBy sort) => sort switch
    {
        OutfitSortBy.Newest => "最新创建",
        OutfitSortBy.Oldest => "最早创建",
        OutfitSortBy.Name => "名称",
        OutfitSortBy.Rating => "评分",
        OutfitSortBy.WearCount => "穿着次数",
        OutfitSortBy.LastWorn => "最近穿着",
        _ => sort.ToString()
    };

    public static string BuildCompactWeatherCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return string.Empty;

        var parts = city
            .Split(" · ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .ToArray();

        return parts.Length == 0 ? city : string.Join(" · ", parts);
    }

    public static string GetSeasonLabel(Season season)
    {
        return season switch
        {
            Season.Spring => "春季",
            Season.Summer => "夏季",
            Season.Autumn => "秋季",
            Season.Winter => "冬季",
            Season.AllSeason => "四季",
            _ => "当前"
        };
    }

    public static string BuildTodayHeroSupportText(
        RecommendedOutfitDto recommendation,
        bool hasTodayWornRecords,
        int todayWornCount)
    {
        if (recommendation.IsWornToday)
            return "这套今天已经记过一次了；如果晚点还要出门，也可以继续穿它。";

        var summary = ResolveHeroSummaryText(recommendation);
        if (!hasTodayWornRecords)
            return summary;

        return $"{summary} 今天已经记过 {todayWornCount} 套，下一套可以换个感觉。";
    }

    public static string ResolveHeroSummaryText(RecommendedOutfitDto recommendation)
    {
        var summary = recommendation.ReasonSummaryText?.Trim();
        if (!string.IsNullOrWhiteSpace(summary))
            return summary;

        var primary = recommendation.PrimaryReason?.Trim();
        return string.IsNullOrWhiteSpace(primary) ? "今天先穿这套。" : primary;
    }
}
