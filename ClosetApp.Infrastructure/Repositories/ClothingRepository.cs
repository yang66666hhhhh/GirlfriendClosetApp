using Microsoft.EntityFrameworkCore;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Infrastructure.Repositories;

public class ClothingRepository : IClothingRepository
{
    private readonly Data.ClosetDbContext _context;

    public ClothingRepository(Data.ClosetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Clothing>> GetAllAsync()
    {
        return await _context.Clothes
            .Include(c => c.ClothingTags)
            .ThenInclude(ct => ct.Tag)
            .ToListAsync();
    }

    public async Task<Clothing?> GetByIdAsync(Guid id)
    {
        return await _context.Clothes
            .Include(c => c.ClothingTags)
            .ThenInclude(ct => ct.Tag)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task AddAsync(Clothing entity)
    {
        _context.Clothes.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Clothing> clothes)
    {
        var items = clothes.ToList();
        if (items.Count == 0)
            return;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Clothes.AddRange(items);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task UpdateAsync(Clothing entity)
    {
        _context.Clothes.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var clothing = await _context.Clothes.FindAsync(id);
        if (clothing != null)
        {
            _context.Clothes.Remove(clothing);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Clothing>> GetByTypeAsync(ClothingType type)
    {
        return await _context.Clothes
            .Where(c => c.Type == type)
            .ToListAsync();
    }

    public async Task<IEnumerable<Clothing>> GetByTypesAsync(IEnumerable<ClothingType> types)
    {
        var selectedTypes = types.Distinct().ToList();
        if (selectedTypes.Count == 0)
            return [];

        return await _context.Clothes
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
        var clothes = await _context.Clothes
            .Where(clothing => selectedIds.Contains(clothing.Id))
            .ToListAsync();

        if (clothes.Count == 0)
            return;

        _context.Clothes.RemoveRange(clothes);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
