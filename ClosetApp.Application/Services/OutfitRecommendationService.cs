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
        var wardrobeProfile = BuildWardrobeProfile(outfits);

        return outfits
            .Where(outfit => outfit.OutfitClothes.Count > 0)
            .Select(outfit => BuildRecommendation(outfit, temperature, scene, wardrobeProfile))
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

    public async Task<RecommendationDebugDto?> GetRecommendationDebugAsync(int temperature, OutfitScene? scene = null)
    {
        var outfits = (await _outfitRepository.GetAllAsync()).ToList();
        var wardrobeProfile = BuildWardrobeProfile(outfits);

        var best = outfits
            .Where(outfit => outfit.OutfitClothes.Count > 0)
            .Select(outfit => BuildDebugRecommendation(outfit, temperature, scene, wardrobeProfile))
            .OrderByDescending(r => r.TotalScore)
            .ThenBy(r => r.Reasons.Count)
            .FirstOrDefault();

        if (best == null)
            return null;

        return new RecommendationDebugDto(
            best.OutfitName,
            best.TotalScore,
            best.BaseScore,
            best.SeasonScore,
            best.FavoriteScore,
            best.RecentWearScore,
            best.WearCountScore,
            best.SceneScore,
            best.PreferenceSceneScore,
            best.PreferenceTagScore,
            best.PreferenceColorScore,
            best.Reasons,
            wardrobeProfile.SceneWeights,
            wardrobeProfile.TagWeights,
            wardrobeProfile.ColorWeights,
            wardrobeProfile.TotalPreferenceWeight);
    }

    private static RecommendationDebugDto BuildDebugRecommendation(
        Outfit outfit,
        int temperature,
        OutfitScene? scene,
        WardrobePreferenceProfile wardrobeProfile)
    {
        var baseScore = outfit.Rating * 12;
        var reasons = new List<string>();

        var seasonScore = ScoreSeason(outfit.Season, temperature, reasons);
        var favoriteScore = ScoreFavorite(outfit, reasons);
        var recentWearScore = ScoreRecentWear(outfit, reasons);
        var wearCountScore = ScoreWearCount(outfit, reasons);
        var preferenceResult = ScorePreferenceDebug(outfit, wardrobeProfile, reasons);

        var sceneScore = 0;
        if (scene.HasValue)
            sceneScore = ScoreScene(outfit, scene.Value, reasons);

        if (reasons.Count == 0)
            reasons.Add("按当前天气和穿着记录看，它今天比较顺手。");

        var totalScore = baseScore + seasonScore + favoriteScore + recentWearScore
            + wearCountScore + sceneScore + preferenceResult.Total;

        return new RecommendationDebugDto(
            outfit.Name,
            totalScore,
            baseScore,
            seasonScore,
            favoriteScore,
            recentWearScore,
            wearCountScore,
            sceneScore,
            preferenceResult.Scene,
            preferenceResult.Tag,
            preferenceResult.Color,
            reasons,
            wardrobeProfile.SceneWeights,
            wardrobeProfile.TagWeights,
            wardrobeProfile.ColorWeights,
            wardrobeProfile.TotalPreferenceWeight);
    }

    private static PreferenceScoreResult ScorePreferenceDebug(
        Outfit outfit,
        WardrobePreferenceProfile profile,
        List<string> reasons)
    {
        if (profile.TotalPreferenceWeight <= 0)
            return PreferenceScoreResult.Zero;

        var sceneScore = 0;
        var tagScore = 0;
        var colorScore = 0;

        if (profile.SceneWeights.TryGetValue(outfit.Scene, out var sceneWeight) && sceneWeight >= 3)
        {
            sceneScore = 8;
            reasons.Add("它贴近你最近常穿/收藏的场景。");
        }

        var tagNames = outfit.OutfitClothes
            .SelectMany(link => link.Clothing.ClothingTags)
            .Select(link => link.Tag.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (tagNames.Any(tag => profile.TagWeights.TryGetValue(tag, out var weight) && weight >= 2))
        {
            tagScore = 7;
            reasons.Add("风格标签也比较贴近你的常用偏好。");
        }

        var colors = outfit.OutfitClothes
            .Select(link => NormalizeColor(link.Clothing.Color))
            .Where(color => color != null)
            .Select(color => color!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (colors.Any(color => profile.ColorWeights.TryGetValue(color, out var weight) && weight >= 2))
        {
            colorScore = 5;
            reasons.Add("颜色也接近你常选的那一类。");
        }

        return new PreferenceScoreResult(sceneScore, tagScore, colorScore);
    }

    private sealed record PreferenceScoreResult(int Scene, int Tag, int Color)
    {
        public int Total => Scene + Tag + Color;
        public static PreferenceScoreResult Zero => new(0, 0, 0);
    }

    private static RecommendedOutfitDto BuildRecommendation(
        Outfit outfit,
        int temperature,
        OutfitScene? scene,
        WardrobePreferenceProfile wardrobeProfile)
    {
        // Keep the scoring rule-based and explainable so the UI can show clear recommendation reasons.
        var score = outfit.Rating * 12;
        var reasons = new List<string>();

        score += ScoreSeason(outfit.Season, temperature, reasons);
        score += ScoreFavorite(outfit, reasons);
        score += ScoreRecentWear(outfit, reasons);
        score += ScoreWearCount(outfit, reasons);
        score += ScorePreference(outfit, wardrobeProfile, reasons);

        if (scene.HasValue)
            score += ScoreScene(outfit, scene.Value, reasons);

        if (reasons.Count == 0)
            reasons.Add("按当前天气和穿着记录看，它今天比较顺手。");

        return new RecommendedOutfitDto(
            outfit,
            score,
            reasons[0],
            reasons.Count > 1 ? reasons[1] : null,
            reasons);
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
        if (daysSinceLastWorn <= 0)
        {
            reasons.Add("今天已经穿过一次了，我先把它往后放一放。");
            return -48;
        }

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

    private static int ScorePreference(Outfit outfit, WardrobePreferenceProfile profile, List<string> reasons)
    {
        if (profile.TotalPreferenceWeight <= 0)
            return 0;

        var score = 0;

        if (profile.SceneWeights.TryGetValue(outfit.Scene, out var sceneWeight) && sceneWeight >= 3)
        {
            score += 8;
            reasons.Add("它贴近你最近常穿/收藏的场景。");
        }

        var tagNames = outfit.OutfitClothes
            .SelectMany(link => link.Clothing.ClothingTags)
            .Select(link => link.Tag.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (tagNames.Any(tag => profile.TagWeights.TryGetValue(tag, out var weight) && weight >= 2))
        {
            score += 7;
            reasons.Add("风格标签也比较贴近你的常用偏好。");
        }

        var colors = outfit.OutfitClothes
            .Select(link => NormalizeColor(link.Clothing.Color))
            .Where(color => color != null)
            .Select(color => color!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (colors.Any(color => profile.ColorWeights.TryGetValue(color, out var weight) && weight >= 2))
        {
            score += 5;
            reasons.Add("颜色也接近你常选的那一类。");
        }

        return score;
    }

    private static WardrobePreferenceProfile BuildWardrobeProfile(IEnumerable<Outfit> outfits)
    {
        var sceneWeights = new Dictionary<OutfitScene, int>();
        var tagWeights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var colorWeights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var totalWeight = 0;

        foreach (var outfit in outfits)
        {
            var weight = outfit.WearCount + outfit.Favorites.Count * 3;
            if (weight <= 0)
                continue;

            totalWeight += weight;
            AddWeight(sceneWeights, outfit.Scene, weight);

            foreach (var tagName in outfit.OutfitClothes
                .SelectMany(link => link.Clothing.ClothingTags)
                .Select(link => link.Tag.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                AddWeight(tagWeights, tagName, weight);
            }

            foreach (var color in outfit.OutfitClothes
                .Select(link => NormalizeColor(link.Clothing.Color))
                .Where(color => color != null)
                .Select(color => color!)
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                AddWeight(colorWeights, color, weight);
            }
        }

        return new WardrobePreferenceProfile(sceneWeights, tagWeights, colorWeights, totalWeight);
    }

    private static string? NormalizeColor(string? color)
    {
        return string.IsNullOrWhiteSpace(color) ? null : color.Trim();
    }

    private static void AddWeight<TKey>(Dictionary<TKey, int> weights, TKey key, int weight)
        where TKey : notnull
    {
        weights[key] = weights.TryGetValue(key, out var current)
            ? current + weight
            : weight;
    }

    private sealed record WardrobePreferenceProfile(
        IReadOnlyDictionary<OutfitScene, int> SceneWeights,
        IReadOnlyDictionary<string, int> TagWeights,
        IReadOnlyDictionary<string, int> ColorWeights,
        int TotalPreferenceWeight);
}
