using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.Services;

public class OutfitRecommendationService : IOutfitRecommendationService
{
    private const int MaxRecommendations = 5;
    private readonly IOutfitRepository _outfitRepository;

    public OutfitRecommendationService(IOutfitRepository outfitRepository)
    {
        _outfitRepository = outfitRepository;
    }

    public async Task<RecommendedOutfitDto?> GetRecommendationAsync(int temperature, OutfitScene? scene = null)
    {
        var recommendations = await GetRecommendationsByRuleAsync(temperature, scene);
        return recommendations.FirstOrDefault();
    }

    public async Task<IEnumerable<RecommendedOutfitDto>> GetRecommendationsByRuleAsync(int temperature, OutfitScene? scene = null)
    {
        var outfits = await _outfitRepository.GetAllAsync();

        return outfits
            .Where(outfit => outfit.OutfitClothes.Count > 0)
            .Select(outfit => BuildRecommendation(outfit, temperature, scene))
            .OrderByDescending(recommendation => recommendation.Score)
            .ThenBy(recommendation => recommendation.WornDate ?? DateTime.MinValue)
            .ThenByDescending(recommendation => recommendation.Rating)
            .Take(MaxRecommendations)
            .ToList();
    }

    public async Task<IEnumerable<Outfit>> GetLowWearOutfitsAsync(int count = 5)
    {
        var outfits = await _outfitRepository.GetAllAsync();
        return outfits.OrderBy(o => o.WearCount).Take(count);
    }

    public async Task<IEnumerable<Outfit>> GetUnwornOutfitsAsync()
    {
        var outfits = await _outfitRepository.GetAllAsync();
        return outfits.Where(o => o.WearCount == 0);
    }

    private static RecommendedOutfitDto BuildRecommendation(Outfit outfit, int temperature, OutfitScene? scene)
    {
        // Keep the scoring rule-based and explainable so the UI can show clear recommendation reasons.
        var score = outfit.Rating * 12;
        var reasons = new List<string>();

        score += ScoreSeason(outfit.Season, temperature, reasons);
        score += ScoreFavorite(outfit, reasons);
        score += ScoreRecentWear(outfit, reasons);
        score += ScoreWearCount(outfit, reasons);

        if (scene.HasValue)
            score += ScoreScene(outfit, scene.Value, reasons);

        if (reasons.Count == 0)
            reasons.Add("按当前天气和穿着记录看，它今天比较顺手。");

        return new RecommendedOutfitDto(
            outfit,
            score,
            reasons[0],
            reasons.Count > 1 ? reasons[1] : null);
    }

    private static int ScoreSeason(Season season, int temperature, List<string> reasons)
    {
        var score = season switch
        {
            Season.Summer when temperature >= 26 => 36,
            Season.Summer when temperature >= 22 => 18,
            Season.Summer => -14,
            Season.Spring when temperature >= 16 && temperature <= 24 => 32,
            Season.Spring when temperature >= 12 && temperature <= 27 => 14,
            Season.Spring => -8,
            Season.Autumn when temperature >= 10 && temperature <= 20 => 32,
            Season.Autumn when temperature >= 7 && temperature <= 23 => 14,
            Season.Autumn => -8,
            Season.Winter when temperature <= 8 => 36,
            Season.Winter when temperature <= 13 => 18,
            Season.Winter => -16,
            Season.AllSeason when temperature >= 12 && temperature <= 28 => 20,
            Season.AllSeason => 8,
            Season.Unspecified => 2,
            _ => 0
        };

        if (score >= 30)
            reasons.Add($"温度在 {temperature}°C 左右，这套的季节感正合适。");
        else if (score >= 18)
            reasons.Add("今天这个温度穿它会比较稳妥。");
        else if (season == Season.AllSeason)
            reasons.Add("它是四季型搭配，今天兜底也够自然。");

        return score;
    }

    private static int ScoreFavorite(Outfit outfit, List<string> reasons)
    {
        if (outfit.Favorites.Count == 0)
            return 0;

        reasons.Add("这套被你标记过收藏，值得优先翻出来穿。");
        return 12;
    }

    private static int ScoreRecentWear(Outfit outfit, List<string> reasons)
    {
        if (!outfit.WornDate.HasValue)
        {
            reasons.Add("它还没有穿着记录，适合今天试一试。");
            return 10;
        }

        var daysSinceLastWorn = (DateTime.Today - outfit.WornDate.Value.Date).TotalDays;
        if (daysSinceLastWorn <= 2)
            return -28;

        if (daysSinceLastWorn <= 7)
            return -18;

        if (daysSinceLastWorn >= 21)
        {
            reasons.Add("已经有一阵子没穿了，今天拿出来会很新鲜。");
            return 10;
        }

        return 0;
    }

    private static int ScoreWearCount(Outfit outfit, List<string> reasons)
    {
        if (outfit.WearCount == 0)
            return 0;

        if (outfit.WearCount <= 2)
        {
            reasons.Add("它还没进入高频轮换，适合多给一次机会。");
            return 6;
        }

        if (outfit.WearCount >= 12)
            return -8;

        return 0;
    }

    private static int ScoreScene(Outfit outfit, OutfitScene scene, List<string> reasons)
    {
        if (outfit.Scene != scene)
            return 0;

        reasons.Add("场景也刚好对得上，省得再临时换搭配。");
        return 18;
    }
}
