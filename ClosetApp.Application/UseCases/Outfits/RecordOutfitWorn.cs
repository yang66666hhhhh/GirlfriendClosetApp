using ClosetApp.Application.Interfaces;

namespace ClosetApp.Application.UseCases.Outfits;

public sealed class RecordOutfitWorn
{
    private readonly IOutfitService _outfitService;

    public RecordOutfitWorn(IOutfitService outfitService)
    {
        _outfitService = outfitService;
    }

    public Task ExecuteAsync(Guid outfitId, DateTime wornDate)
    {
        return _outfitService.RecordWornDateAsync(outfitId, wornDate);
    }
}
