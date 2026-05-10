using ClosetApp.Domain.Entities;

namespace ClosetApp.Domain.Interfaces;

public interface IFavoriteRepository : IRepository<Favorite>
{
    Task<IEnumerable<Favorite>> GetByOutfitIdAsync(Guid outfitId);
}
