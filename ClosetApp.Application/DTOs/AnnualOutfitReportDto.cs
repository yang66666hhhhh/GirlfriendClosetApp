using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public sealed record AnnualOutfitReportDto(
    int Year,
    int TotalWearCount,
    int ActiveDays,
    int TotalOutfitCount,
    int WornOutfitCount,
    int FavoriteOutfitCount,
    MonthlyStatsItem BestMonth,
    TopOutfitItem? MostWornOutfit,
    IReadOnlyList<TopOutfitItem> Top5Outfits,
    IReadOnlyList<MonthlyStatsItem> MonthlyStats,
    IReadOnlyList<DistributionItem> SceneDistribution,
    IReadOnlyList<DistributionItem> SeasonDistribution,
    IReadOnlyList<string> Highlights)
{
    public string WearRateText => TotalOutfitCount > 0
        ? $"{WornOutfitCount * 100 / TotalOutfitCount}%"
        : "0%";
    public string ActiveDaysText => $"全年 {ActiveDays} 天有穿搭记录";
    public string BestMonthText => $"{BestMonth.MonthName} 穿得最多，共 {BestMonth.WearCount} 次";
}

public sealed record MonthlyStatsItem(
    int Month,
    int WearCount,
    int ActiveDays)
{
    public string MonthName => Month switch
    {
        1 => "一月", 2 => "二月", 3 => "三月", 4 => "四月",
        5 => "五月", 6 => "六月", 7 => "七月", 8 => "八月",
        9 => "九月", 10 => "十月", 11 => "十一月", 12 => "十二月",
        _ => $"{Month}月"
    };
    public string SummaryText => $"{WearCount} 次 · {ActiveDays} 天";
}

public sealed record TopOutfitItem(
    string Name,
    int WearCount,
    Season Season,
    OutfitScene Scene)
{
    public string SeasonText => Season switch
    {
        Season.Spring => "春",
        Season.Summer => "夏",
        Season.Autumn => "秋",
        Season.Winter => "冬",
        Season.AllSeason => "四季",
        _ => ""
    };
    public string SceneText => Scene switch
    {
        OutfitScene.Work => "通勤",
        OutfitScene.Date => "约会",
        OutfitScene.Travel => "出游",
        OutfitScene.Party => "派对",
        OutfitScene.Casual => "休闲",
        _ => ""
    };
}
