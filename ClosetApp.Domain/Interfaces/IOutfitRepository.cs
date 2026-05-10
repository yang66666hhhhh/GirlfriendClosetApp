using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Domain.Interfaces;

public interface IOutfitRepository : IRepository<Outfit>
{
    Task<IEnumerable<Outfit>> GetBySceneAsync(OutfitScene scene);
    Task<IEnumerable<Outfit>> GetBySeasonAsync(Season season);
    Task<IEnumerable<Outfit>> GetRecentlyWornAsync(int count);
}
