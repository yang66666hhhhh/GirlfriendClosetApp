using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public sealed record WardrobeInsightsDto(
    int TotalOutfitCount,
    int WornOutfitCount,
    int NeverWornCount,
    int WearRate,
    int TotalWearCount,
    int ActiveDays,
    int CurrentStreak,
    IReadOnlyList<TopWornOutfitItem> TopWornOutfits,
    IReadOnlyList<DistributionItem> SceneDistribution,
    IReadOnlyList<DistributionItem> SeasonDistribution,
    IReadOnlyList<IdleOutfitItem> IdleOutfits,
    int IdleThresholdDays)
{
    public string WearRateText => WearRate >= 80 ? "搭配利用率很高" : WearRate >= 50 ? "搭配利用率一般" : "很多搭配还没穿过";
    public string StreakText => CurrentStreak switch
    {
        0 => "今天还没记录穿搭",
        1 => "今天已经记录了穿搭",
        _ => $"连续 {CurrentStreak} 天记录穿搭"
    };
    public string IdleSummaryText => IdleOutfits.Count == 0
        ? "所有搭配近期都有穿过"
        : $"有 {IdleOutfits.Count} 套搭配超过 {IdleThresholdDays} 天没穿了";
}

public sealed record TopWornOutfitItem(
    string Name,
    int WearCount,
    DateTime? LastWornDate)
{
    public string WearCountText => $"穿过 {WearCount} 次";
    public string LastWornText => LastWornDate.HasValue
        ? $"最近 {LastWornDate.Value:MM/dd}"
        : "还没穿过";
}

public sealed record DistributionItem(
    string Label,
    int Count,
    int Total)
{
    public int Percentage => Total > 0 ? Count * 100 / Total : 0;
    public string DisplayText => $"{Label} {Count}";
}

public sealed record IdleOutfitItem(
    Guid Id,
    string Name,
    int DaysSinceLastWorn,
    Season Season)
{
    public string IdleText => DaysSinceLastWorn switch
    {
        <= 14 => "最近两周穿过",
        <= 30 => $"已 {DaysSinceLastWorn} 天没穿",
        <= 90 => $"已 {DaysSinceLastWorn / 30} 个月没穿",
        _ => $"已 {DaysSinceLastWorn / 30} 个月以上没穿"
    };
    public string SeasonText => Season switch
    {
        Season.Spring => "春季",
        Season.Summer => "夏季",
        Season.Autumn => "秋季",
        Season.Winter => "冬季",
        Season.AllSeason => "四季",
        _ => ""
    };
}
