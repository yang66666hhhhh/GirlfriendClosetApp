using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.Services;

public class OutfitRecommendationService : IOutfitRecommendationService
{
    private readonly IOutfitRepository _outfitRepository;

    public OutfitRecommendationService(IOutfitRepository outfitRepository)
    {
        _outfitRepository = outfitRepository;
    }

    public async Task<Outfit?> GetRecommendationAsync(int temperature, OutfitScene? scene = null)
    {
        var recommendations = await GetRecommendationsByRuleAsync(temperature, scene);
        return recommendations.FirstOrDefault();
    }

    public async Task<IEnumerable<Outfit>> GetRecommendationsByRuleAsync(int temperature, OutfitScene? scene = null)
    {
        var outfits = await _outfitRepository.GetAllAsync();

        return outfits
            .Select(o => new
            {
                Outfit = o,
                Score = CalculateScore(o, temperature, scene)
            })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Outfit)
            .Take(5);
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

    private int CalculateScore(Outfit outfit, int temperature, OutfitScene? scene)
    {
        int score = outfit.Rating * 10;

        if (temperature >= 25 && outfit.Season == Season.Summer)
            score += 30;
        else if (temperature >= 15 && temperature < 25 && outfit.Season == Season.Spring)
            score += 30;
        else if (temperature >= 5 && temperature < 15 && outfit.Season == Season.Autumn)
            score += 30;
        else if (temperature < 5 && outfit.Season == Season.Winter)
            score += 30;
        else if (outfit.Season == Season.AllSeason)
            score += 15;

        if (scene.HasValue && outfit.Scene == scene.Value)
            score += 25;

        score += Math.Min(outfit.WearCount, 10);

        return score;
    }
}