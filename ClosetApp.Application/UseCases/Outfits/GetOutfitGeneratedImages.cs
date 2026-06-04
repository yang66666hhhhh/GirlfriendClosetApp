using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.UseCases.Outfits;

public sealed class GetOutfitGeneratedImages
{
    private readonly IOutfitGeneratedImageRepository _repository;

    public GetOutfitGeneratedImages(IOutfitGeneratedImageRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<OutfitGeneratedImageDto>> ExecuteAsync(Guid outfitId)
    {
        var images = await _repository.GetByOutfitIdAsync(outfitId);
        return images
            .OrderByDescending(image => image.CreatedAt)
            .Select(image => image.ToDto())
            .ToList();
    }
}
