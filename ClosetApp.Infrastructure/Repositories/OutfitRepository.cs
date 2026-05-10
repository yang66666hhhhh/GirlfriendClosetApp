using Microsoft.EntityFrameworkCore;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Infrastructure.Repositories;

public class OutfitRepository : IOutfitRepository
{
    private readonly Data.ClosetDbContext _context;

    public OutfitRepository(Data.ClosetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Outfit>> GetAllAsync()
    {
        return await _context.Outfits
            .Include(o => o.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .Include(o => o.WornRecords)
            .ToListAsync();
    }

    public async Task<Outfit?> GetByIdAsync(Guid id)
    {
        return await _context.Outfits
            .Include(o => o.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .Include(o => o.WornRecords)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task AddAsync(Outfit entity)
    {
        _context.Outfits.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Outfit entity)
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var outfit = await _context.Outfits.FindAsync(id);
        if (outfit != null)
        {
            _context.Outfits.Remove(outfit);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Outfit>> GetBySceneAsync(OutfitScene scene)
    {
        return await _context.Outfits
            .Where(o => o.Scene == scene)
            .Include(o => o.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .ToListAsync();
    }

    public async Task<IEnumerable<Outfit>> GetBySeasonAsync(Season season)
    {
        return await _context.Outfits
            .Where(o => o.Season == season)
            .Include(o => o.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .ToListAsync();
    }

    public async Task<IEnumerable<Outfit>> GetRecentlyWornAsync(int count)
    {
        return await _context.Outfits
            .Where(o => o.WornDate != null)
            .OrderByDescending(o => o.WornDate)
            .Take(count)
            .Include(o => o.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .ToListAsync();
    }
}