using Microsoft.EntityFrameworkCore;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Infrastructure.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly Data.ClosetDbContext _context;
    private readonly ICurrentUserContext? _currentUserContext;

    public FavoriteRepository(Data.ClosetDbContext context, ICurrentUserContext? currentUserContext = null)
    {
        _context = context;
        _currentUserContext = currentUserContext;
    }

    public async Task<IEnumerable<Favorite>> GetAllAsync()
    {
        var query = await ForCurrentUserAsync(_context.Favorites);
        return await query.ToListAsync();
    }

    public async Task<Favorite?> GetByIdAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.Favorites);
        return await query.FirstOrDefaultAsync(favorite => favorite.Id == id);
    }

    public async Task AddAsync(Favorite entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.Favorites.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Favorite entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.Favorites.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.Favorites);
        var favorite = await query.FirstOrDefaultAsync(item => item.Id == id);
        if (favorite != null)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Favorite>> GetByOutfitIdAsync(Guid outfitId)
    {
        var query = await ForCurrentUserAsync(_context.Favorites);
        return await query
            .Where(f => f.OutfitId == outfitId)
            .ToListAsync();
    }

    private async Task<IQueryable<Favorite>> ForCurrentUserAsync(IQueryable<Favorite> query)
    {
        if (_currentUserContext == null)
            return query;

        var userId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
        return query.Where(favorite => favorite.LocalUserId == userId);
    }

    private async Task AssignCurrentUserAsync(Favorite entity)
    {
        if (_currentUserContext == null || entity.LocalUserId.HasValue)
            return;

        entity.LocalUserId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
    }
}
