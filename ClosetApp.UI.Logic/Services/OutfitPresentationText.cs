using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Entities;
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

    public static string BuildCompactWeatherSummary(int temperature, string condition)
    {
        return $"{temperature}°C · {condition}";
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

    public static string BuildRecommendationCountText(IReadOnlyList<RecommendedOutfitDto> recommendations)
    {
        return recommendations.Count > 0 ? $"{recommendations.Count} 套" : "暂无";
    }

    public static int CountTodayWornRecords(IReadOnlyList<RecentWornListItem> recentWornRecords)
    {
        return recentWornRecords.Count(record => record.WornDate.Date == DateTime.Today);
    }

    public static string BuildTodayWornStatusText(int todayWornCount)
    {
        return todayWornCount > 0 ? $"今天已记 {todayWornCount} 套" : "今天还没记录";
    }

    public static string BuildHistoryQuickText(int recentRecordCount)
    {
        return recentRecordCount == 0 ? "暂无记录" : $"{recentRecordCount} 条最近记录";
    }

    public static string BuildHistorySummaryText(int recentRecordCount)
    {
        return recentRecordCount == 0
            ? "记录一次「今天穿了」，这里就会生成你的穿搭时间线。"
            : $"最近 {recentRecordCount} 条穿着记录，点日历日期可以补记或撤销。";
    }

    public static string BuildDefaultCalendarSummaryText()
    {
        return "按月份回看每天穿了哪套，慢慢就会长出你的穿搭习惯。";
    }

    public static string BuildCalendarSummaryText(IReadOnlyList<OutfitWornRecord> records)
    {
        if (records.Count == 0)
            return "这个月还没有穿搭记录。点任意一天，可以补记那天穿了什么。";

        var activeDays = records.Select(record => record.WornDate.Date).Distinct().Count();
        var mostWorn = records
            .GroupBy(record => record.Outfit?.Name ?? "未命名搭配")
            .OrderByDescending(group => group.Count())
            .First();

        return $"本月 {records.Count} 次记录 · {activeDays} 天有穿搭 · 最常穿「{mostWorn.Key}」";
    }

    public static string BuildRecommendationReadinessBadgeText(bool hasRecommendationGap)
    {
        return hasRecommendationGap ? "还差一点" : "已经就绪";
    }

    public static string BuildRecommendationReadinessCountText(RecommendationReadinessSummaryDto? readiness)
    {
        if (readiness == null)
            return "等待刷新";

        return readiness.MatchingSeasonCount > 0
            ? $"{readiness.MatchingSeasonCount}/{readiness.ReadyOutfitCount} 套对季"
            : $"{readiness.ReadyOutfitCount} 套已整理";
    }

    public static string BuildRecommendationMissingSeasonText(
        RecommendationReadinessSummaryDto? readiness,
        bool hasRecommendationGap)
    {
        if (readiness?.MissingSeason is { } season)
            return $"建议补 {GetSeasonLabel(season)} 搭配";

        return hasRecommendationGap
            ? "先把常穿搭配补完整，推荐会更稳。"
            : "当前温度下已经有可轮换的搭配。";
    }

    public static string BuildWeatherRecommendationHintText(
        IReadOnlyList<RecommendedOutfitDto> recommendations,
        string recommendationReadinessDetail)
    {
        return recommendations.Count == 0
            ? recommendationReadinessDetail
            : recommendations[0].PrimaryReason;
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

    public static string BuildTodayHeroPrimaryActionText(
        RecommendedOutfitDto? primaryRecommendation,
        bool hasTodayWornRecords)
    {
        if (primaryRecommendation == null)
            return "去新建一套";

        if (primaryRecommendation.IsWornToday)
            return "今天又穿它";

        return hasTodayWornRecords ? "再记这套" : "今天穿它";
    }

    public static IReadOnlyList<string> BuildSecondaryRecommendationReasonTags(RecommendedOutfitDto recommendation)
    {
        var tags = new List<string>();

        if (recommendation.Reasons.Any(reason =>
                reason.Contains("收藏", StringComparison.OrdinalIgnoreCase) ||
                reason.Contains("偏爱", StringComparison.OrdinalIgnoreCase)))
        {
            tags.Add("你的偏爱");
        }

        if (recommendation.Reasons.Any(reason =>
                reason.Contains("场景", StringComparison.OrdinalIgnoreCase)))
        {
            tags.Add("场景贴合");
        }

        if (recommendation.Reasons.Any(reason =>
                reason.Contains("颜色", StringComparison.OrdinalIgnoreCase)))
        {
            tags.Add("颜色顺手");
        }

        if (recommendation.Reasons.Any(reason =>
                reason.Contains("没穿", StringComparison.OrdinalIgnoreCase) ||
                reason.Contains("新鲜", StringComparison.OrdinalIgnoreCase) ||
                reason.Contains("轮换", StringComparison.OrdinalIgnoreCase)))
        {
            tags.Add("还没穿过");
        }

        if (tags.Count == 0)
        {
            tags.AddRange(
                recommendation.HighlightTags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
        }

        return tags
            .Distinct(StringComparer.Ordinal)
            .Take(2)
            .ToList();
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
