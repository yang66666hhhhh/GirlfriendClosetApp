using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IFavoriteRepository _repository;

    public FavoriteService(IFavoriteRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Favorite>> GetAllFavoritesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Favorite> AddFavoriteAsync(Favorite favorite)
    {
        await _repository.AddAsync(favorite);
        return favorite;
    }

    public async Task RemoveFavoriteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<bool> IsFavoriteAsync(Guid outfitId)
    {
        var favorites = await _repository.GetAllAsync();
        return favorites.Any(f => f.OutfitId == outfitId);
    }
}