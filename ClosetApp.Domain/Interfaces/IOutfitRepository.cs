using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Domain.Interfaces;

public interface IOutfitRepository : IRepository<Outfit>
{
    Task<IEnumerable<Outfit>> GetBySceneAsync(OutfitScene scene);
    Task<IEnumerable<Outfit>> GetBySeasonAsync(Season season);
    Task<IEnumerable<Outfit>> GetRecentlyWornAsync(int count);
    Task<IEnumerable<Outfit>> GetOutfitsByClothingIdAsync(Guid clothingId);
    Task DeleteEmptyOutfitsAsync();
    Task<List<OutfitUpdateResult>> DeleteInvalidOutfitsAsync(Guid excludedClothingId);
}

public class OutfitUpdateResult
{
    public Guid OutfitId { get; set; }
    public string OutfitName { get; set; } = string.Empty;
    public int RemainingClothingCount { get; set; }
    public bool WasDeleted { get; set; }
}
