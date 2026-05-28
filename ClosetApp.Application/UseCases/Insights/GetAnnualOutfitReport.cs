using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.UseCases.Insights;

public sealed class GetAnnualOutfitReport
{
    private readonly IOutfitService _outfitService;

    public GetAnnualOutfitReport(IOutfitService outfitService)
    {
        _outfitService = outfitService;
    }

    public async Task<AnnualOutfitReportDto> ExecuteAsync(int year)
    {
        var outfits = (await _outfitService.GetAllOutfitsAsync()).ToList();
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = yearStart.AddYears(1).AddTicks(-1);

        var wornRecords = await _outfitService.GetWornRecordsAsync(yearStart, yearEnd);
        var recordsList = wornRecords.ToList();

        var totalWearCount = recordsList.Count;
        var activeDays = recordsList.Select(r => r.WornDate.Date).Distinct().Count();
        var totalOutfitCount = outfits.Count;
        var wornOutfitCount = outfits.Count(o => o.WearCount > 0);
        var favoriteOutfitCount = outfits.Count(o => o.Favorites.Count > 0);

        var monthlyStats = recordsList
            .GroupBy(r => r.WornDate.Month)
            .Select(g => new MonthlyStatsItem(
                g.Key,
                g.Count(),
                g.Select(r => r.WornDate.Date).Distinct().Count()))
            .OrderBy(m => m.Month)
            .ToList();

        var bestMonth = monthlyStats.Count > 0
            ? monthlyStats.OrderByDescending(m => m.WearCount).First()
            : new MonthlyStatsItem(DateTime.Now.Month, 0, 0);

        var topOutfits = recordsList
            .GroupBy(r => r.OutfitId)
            .Select(g =>
            {
                var outfit = outfits.FirstOrDefault(o => o.Id == g.Key);
                return new TopOutfitItem(
                    outfit?.Name ?? "未命名",
                    g.Count(),
                    outfit?.Season ?? Season.Unspecified,
                    outfit?.Scene ?? OutfitScene.Casual);
            })
            .OrderByDescending(t => t.WearCount)
            .Take(5)
            .ToList();

        var mostWornOutfit = topOutfits.FirstOrDefault();

        var sceneDistribution = recordsList
            .Select(r => outfits.FirstOrDefault(o => o.Id == r.OutfitId)?.Scene ?? OutfitScene.Casual)
            .GroupBy(s => s)
            .Select(g => new DistributionItem(GetSceneLabel(g.Key), g.Count(), totalWearCount))
            .OrderByDescending(d => d.Count)
            .ToList();

        var seasonDistribution = recordsList
            .Select(r => outfits.FirstOrDefault(o => o.Id == r.OutfitId)?.Season ?? Season.Unspecified)
            .GroupBy(s => s)
            .Select(g => new DistributionItem(GetSeasonLabel(g.Key), g.Count(), totalWearCount))
            .OrderByDescending(d => d.Count)
            .ToList();

        var highlights = BuildHighlights(year, totalWearCount, activeDays, bestMonth, mostWornOutfit, topOutfits);

        return new AnnualOutfitReportDto(
            year,
            totalWearCount,
            activeDays,
            totalOutfitCount,
            wornOutfitCount,
            favoriteOutfitCount,
            bestMonth,
            mostWornOutfit,
            topOutfits,
            monthlyStats,
            sceneDistribution,
            seasonDistribution,
            highlights);
    }

    private static List<string> BuildHighlights(
        int year,
        int totalWearCount,
        int activeDays,
        MonthlyStatsItem bestMonth,
        TopOutfitItem? mostWornOutfit,
        IReadOnlyList<TopOutfitItem> topOutfits)
    {
        var highlights = new List<string>();

        if (totalWearCount > 0)
            highlights.Add($"全年共记录 {totalWearCount} 次穿搭");

        if (activeDays > 0)
            highlights.Add($"其中 {activeDays} 天有穿搭记录");

        if (bestMonth.WearCount > 0)
            highlights.Add($"{bestMonth.MonthName} 是穿搭最活跃的月份");

        if (mostWornOutfit != null)
            highlights.Add($"「{mostWornOutfit.Name}」是全年最常穿的搭配");

        if (topOutfits.Count >= 3)
            highlights.Add($"Top 3 搭配共穿着 {topOutfits.Take(3).Sum(t => t.WearCount)} 次");

        return highlights;
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
