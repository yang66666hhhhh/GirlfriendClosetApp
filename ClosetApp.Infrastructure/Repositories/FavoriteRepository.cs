using Microsoft.EntityFrameworkCore;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Infrastructure.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly Data.ClosetDbContext _context;

    public FavoriteRepository(Data.ClosetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Favorite>> GetAllAsync()
    {
        return await _context.Favorites.ToListAsync();
    }

    public async Task<Favorite?> GetByIdAsync(Guid id)
    {
        return await _context.Favorites.FindAsync(id);
    }

    public async Task AddAsync(Favorite entity)
    {
        _context.Favorites.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Favorite entity)
    {
        _context.Favorites.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var favorite = await _context.Favorites.FindAsync(id);
        if (favorite != null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Favorite>> GetByOutfitIdAsync(Guid outfitId)
    {
        return await _context.Favorites
            .Where(f => f.OutfitId == outfitId)
            .ToListAsync();
    }
}