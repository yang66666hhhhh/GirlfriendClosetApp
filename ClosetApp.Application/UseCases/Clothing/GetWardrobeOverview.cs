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
            .GroupBy(ResolveDisplayCategory)
            .ToDictionary(group => group.Key, group => group.Count());

        return new WardrobeOverviewResult(
            clothes.Count,
            clothes.Count(c => c.IsFavorite),
            byCategory);
    }

    private static DisplayCategory ResolveDisplayCategory(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        if (clothing.GarmentType.HasValue)
            return ClothingMappings.GetDisplayCategory(clothing.GarmentType.Value);
        return ClothingMappings.GetDisplayCategory(ClothingMappings.InferGarmentType(clothing.Type));
    }
}

public sealed record WardrobeOverviewResult(
    int TotalCount,
    int FavoriteCount,
    IReadOnlyDictionary<DisplayCategory, int> CountByCategory);
