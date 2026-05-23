using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;

namespace ClosetApp.Application.UseCases.Clothing;

public sealed class GetWardrobeOverview
{
    private readonly IClothingService _clothingService;

    public GetWardrobeOverview(IClothingService clothingService)
    {
        _clothingService = clothingService;
    }

    public async Task<WardrobeOverviewResult> ExecuteAsync()
    {
        var clothes = (await _clothingService.GetAllClothesAsync()).ToList();
        var byCategory = clothes
            .Select(ClothingMappings.TryGetDisplayCategory)
            .Where(category => category.HasValue)
            .Select(category => category!.Value)
            .GroupBy(category => category)
            .ToDictionary(group => group.Key, group => group.Count());

        return new WardrobeOverviewResult(
            clothes.Count,
            clothes.Count(c => c.FavoriteLevel >= 4),
            byCategory);
    }

}

public sealed record WardrobeOverviewResult(
    int TotalCount,
    int FavoriteCount,
    IReadOnlyDictionary<DisplayCategory, int> CountByCategory);
