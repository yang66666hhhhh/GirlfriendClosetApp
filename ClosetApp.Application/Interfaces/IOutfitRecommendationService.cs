using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.Interfaces;

public interface IOutfitRecommendationService
{
    Task<Outfit?> GetRecommendationAsync(int temperature, OutfitScene? scene = null);
    Task<IEnumerable<Outfit>> GetRecommendationsByRuleAsync(int temperature, OutfitScene? scene = null);
    Task<IEnumerable<Outfit>> GetLowWearOutfitsAsync(int count = 5);
    Task<IEnumerable<Outfit>> GetUnwornOutfitsAsync();
}