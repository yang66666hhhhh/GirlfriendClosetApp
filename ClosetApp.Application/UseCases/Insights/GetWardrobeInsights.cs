using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.UseCases.Insights;

public sealed class GetWardrobeInsights
{
    private const int IdleThresholdDays = 14;
    private const int TopCount = 5;
    private const int IdleListLimit = 8;

    private readonly IOutfitService _outfitService;

    public GetWardrobeInsights(IOutfitService outfitService)
    {
        _outfitService = outfitService;
    }

    public async Task<WardrobeInsightsDto> ExecuteAsync()
    {
        var outfits = (await _outfitService.GetAllOutfitsAsync()).ToList();
        var today = DateTime.Today;

        var totalOutfitCount = outfits.Count;
        var wornOutfits = outfits.Where(o => o.WearCount > 0).ToList();
        var wornOutfitCount = wornOutfits.Count;
        var neverWornCount = totalOutfitCount - wornOutfitCount;
        var wearRate = totalOutfitCount > 0 ? wornOutfitCount * 100 / totalOutfitCount : 0;
        var totalWearCount = outfits.Sum(o => o.WearCount);

        var activeDays = outfits
            .Where(o => o.WornDate.HasValue)
            .Select(o => o.WornDate!.Value.Date)
            .Distinct()
            .Count();

        var currentStreak = CalculateStreak(outfits, today);

        var topWornOutfits = outfits
            .Where(o => o.WearCount > 0)
            .OrderByDescending(o => o.WearCount)
            .ThenByDescending(o => o.WornDate)
            .Take(TopCount)
            .Select(o => new TopWornOutfitItem(o.Name, o.WearCount, o.WornDate))
            .ToList();

        var sceneDistribution = outfits
            .GroupBy(o => o.Scene)
            .Select(g => new DistributionItem(GetSceneLabel(g.Key), g.Count(), totalOutfitCount))
            .OrderByDescending(d => d.Count)
            .ToList();

        var seasonDistribution = outfits
            .GroupBy(o => o.Season)
            .Select(g => new DistributionItem(GetSeasonLabel(g.Key), g.Count(), totalOutfitCount))
            .OrderByDescending(d => d.Count)
            .ToList();

        var idleOutfits = outfits
            .Where(o => o.WearCount > 0 && o.WornDate.HasValue)
            .Select(o => new
            {
                Outfit = o,
                DaysSinceLastWorn = (today - o.WornDate!.Value.Date).Days
            })
            .Where(x => x.DaysSinceLastWorn > IdleThresholdDays)
            .OrderByDescending(x => x.DaysSinceLastWorn)
            .Take(IdleListLimit)
            .Select(x => new IdleOutfitItem(x.Outfit.Id, x.Outfit.Name, x.DaysSinceLastWorn, x.Outfit.Season))
            .ToList();

        return new WardrobeInsightsDto(
            totalOutfitCount,
            wornOutfitCount,
            neverWornCount,
            wearRate,
            totalWearCount,
            activeDays,
            currentStreak,
            topWornOutfits,
            sceneDistribution,
            seasonDistribution,
            idleOutfits,
            IdleThresholdDays);
    }

    private static int CalculateStreak(List<Domain.Entities.Outfit> outfits, DateTime today)
    {
        var wornDates = outfits
            .Where(o => o.WornDate.HasValue)
            .Select(o => o.WornDate!.Value.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (wornDates.Count == 0 || wornDates[0] != today)
            return 0;

        var streak = 1;
        for (var i = 1; i < wornDates.Count; i++)
        {
            if (wornDates[i] == wornDates[i - 1].AddDays(-1))
                streak++;
            else
                break;
        }

        return streak;
    }

    private static string GetSceneLabel(OutfitScene scene) => scene switch
    {
        OutfitScene.Work => "通勤",
        OutfitScene.Date => "约会",
        OutfitScene.Travel => "出游",
        OutfitScene.Party => "派对",
        OutfitScene.Casual => "休闲",
        _ => scene.ToString()
    };

    private static string GetSeasonLabel(Season season) => season switch
    {
        Season.Spring => "春季",
        Season.Summer => "夏季",
        Season.Autumn => "秋季",
        Season.Winter => "冬季",
        Season.AllSeason => "四季",
        _ => "未分类"
    };
}
