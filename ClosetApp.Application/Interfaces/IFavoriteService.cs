using ClosetApp.Domain.Entities;

namespace ClosetApp.Application.Interfaces;

public interface IFavoriteService
{
    Task<IEnumerable<Favorite>> GetAllFavoritesAsync();
    Task<Favorite> AddFavoriteAsync(Favorite favorite);
    Task RemoveFavoriteAsync(Guid id);
    Task<bool> IsFavoriteAsync(Guid outfitId);
}