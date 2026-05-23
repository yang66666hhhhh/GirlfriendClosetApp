using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.Interfaces;

public interface IOutfitRecommendationService
{
    Task<RecommendedOutfitDto?> GetRecommendationAsync(int temperature, OutfitScene? scene = null);
    Task<IEnumerable<RecommendedOutfitDto>> GetRecommendationsByRuleAsync(int temperature, OutfitScene? scene = null);
    Task<IEnumerable<Outfit>> GetLowWearOutfitsAsync(int count = 5);
    Task<IEnumerable<Outfit>> GetUnwornOutfitsAsync();
}
