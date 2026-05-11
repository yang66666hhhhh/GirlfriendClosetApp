using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;

namespace ClosetApp.Application.UseCases.Insights;

public sealed class GetOutfitHistorySummary
{
    private readonly IOutfitService _outfitService;

    public GetOutfitHistorySummary(IOutfitService outfitService)
    {
        _outfitService = outfitService;
    }

    public async Task<OutfitHistorySummary> ExecuteAsync()
    {
        var outfits = (await _outfitService.GetAllOutfitsAsync()).ToList();
        var totalWearCount = outfits.Sum(o => o.WearCount);
        var lastWorn = outfits
            .Where(o => o.WornDate.HasValue)
            .OrderByDescending(o => o.WornDate)
            .FirstOrDefault();

        return new OutfitHistorySummary(
            totalWearCount,
            lastWorn,
            outfits.OrderByDescending(o => o.WearCount).Take(5).ToList());
    }
}

public sealed record OutfitHistorySummary(
    int TotalWearCount,
    Outfit? LastWornOutfit,
    IReadOnlyList<Outfit> MostWornOutfits);
