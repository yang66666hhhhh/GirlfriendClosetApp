using ClosetApp.Domain.Entities;

namespace ClosetApp.Domain.Interfaces;

public interface IOutfitGeneratedImageRepository : IRepository<OutfitGeneratedImage>
{
    Task<IReadOnlyList<OutfitGeneratedImage>> GetByOutfitIdAsync(Guid outfitId);
    Task<OutfitGeneratedImage?> GetPrimaryByOutfitIdAsync(Guid outfitId);
    Task ClearPrimaryAsync(Guid outfitId, Guid? excludingId = null);
}
