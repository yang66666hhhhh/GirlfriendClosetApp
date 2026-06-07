using Microsoft.EntityFrameworkCore;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Infrastructure.Repositories;

public class ClothingRepository : IClothingRepository
{
    private readonly Data.ClosetDbContext _context;
    private readonly ICurrentUserContext? _currentUserContext;

    public ClothingRepository(Data.ClosetDbContext context, ICurrentUserContext? currentUserContext = null)
    {
        _context = context;
        _currentUserContext = currentUserContext;
    }

    public async Task<IEnumerable<Clothing>> GetAllAsync()
    {
        var query = await ForCurrentUserAsync(_context.Clothes);
        return await query
            .Include(c => c.ClothingTags)
            .ThenInclude(ct => ct.Tag)
            .ToListAsync();
    }

    public async Task<Clothing?> GetByIdAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.Clothes);
        return await query
            .Include(c => c.ClothingTags)
            .ThenInclude(ct => ct.Tag)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task AddAsync(Clothing entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.Clothes.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Clothing> clothes)
    {
        var items = clothes.ToList();
        if (items.Count == 0)
            return;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        foreach (var item in items)
            await AssignCurrentUserAsync(item);

        _context.Clothes.AddRange(items);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task UpdateAsync(Clothing entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.Clothes.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.Clothes);
        var clothing = await query.FirstOrDefaultAsync(item => item.Id == id);
        if (clothing != null)
        {
            _context.Clothes.Remove(clothing);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Clothing>> GetByTypeAsync(ClothingType type)
    {
        var query = await ForCurrentUserAsync(_context.Clothes);
        return await query
            .Where(c => c.Type == type)
            .ToListAsync();
    }

    public async Task<IEnumerable<Clothing>> GetByTypesAsync(IEnumerable<ClothingType> types)
    {
        var selectedTypes = types.Distinct().ToList();
        if (selectedTypes.Count == 0)
            return [];

        var query = await ForCurrentUserAsync(_context.Clothes);
        return await query
            .Where(clothing => selectedTypes.Contains(clothing.Type))
            .Include(clothing => clothing.ClothingTags)
            .ThenInclude(clothingTag => clothingTag.Tag)
            .ToListAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<Guid> ids)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count == 0)
            return;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var query = await ForCurrentUserAsync(_context.Clothes);
        var clothes = await query
            .Where(clothing => selectedIds.Contains(clothing.Id))
            .ToListAsync();

        if (clothes.Count == 0)
            return;

        _context.Clothes.RemoveRange(clothes);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task<IQueryable<Clothing>> ForCurrentUserAsync(IQueryable<Clothing> query)
    {
        if (_currentUserContext == null)
            return query;

        var userId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
        return query.Where(clothing => clothing.LocalUserId == userId);
    }

    private async Task AssignCurrentUserAsync(Clothing entity)
    {
        if (_currentUserContext == null || entity.LocalUserId.HasValue)
            return;

        entity.LocalUserId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
    }
}
