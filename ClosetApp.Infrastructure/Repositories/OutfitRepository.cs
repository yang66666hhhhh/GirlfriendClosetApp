using Microsoft.EntityFrameworkCore;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;
using Serilog;

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
            .AsNoTracking()
            .Include(o => o.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .ThenInclude(c => c.ClothingTags)
            .ThenInclude(ct => ct.Tag)
            .Include(o => o.Favorites)
            .Include(o => o.WornRecords)
            .ToListAsync();
    }

    public async Task<Outfit?> GetByIdAsync(Guid id)
    {
        return await _context.Outfits
            .AsNoTracking()
            .Include(o => o.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .ThenInclude(c => c.ClothingTags)
            .ThenInclude(ct => ct.Tag)
            .Include(o => o.Favorites)
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
        var clothingIds = entity.OutfitClothes
            .Select(oc => oc.ClothingId)
            .Distinct()
            .ToList();

        var outfit = await _context.Outfits
            .Include(o => o.OutfitClothes)
            .FirstOrDefaultAsync(o => o.Id == entity.Id);

        if (outfit == null)
            return;

        outfit.Name = entity.Name;
        outfit.Scene = entity.Scene;
        outfit.Season = entity.Season;
        outfit.Rating = entity.Rating;
        outfit.Notes = entity.Notes;
        outfit.WornDate = entity.WornDate;
        outfit.WearCount = entity.WearCount;

        var existingLinks = outfit.OutfitClothes.ToList();
        foreach (var link in existingLinks.Where(link => !clothingIds.Contains(link.ClothingId)))
            outfit.OutfitClothes.Remove(link);

        var existingIds = outfit.OutfitClothes.Select(oc => oc.ClothingId).ToHashSet();
        foreach (var clothingId in clothingIds.Where(id => !existingIds.Contains(id)))
        {
            outfit.OutfitClothes.Add(new OutfitClothing
            {
                OutfitId = outfit.Id,
                ClothingId = clothingId
            });
        }

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

    public async Task DeleteEmptyOutfitsAsync()
    {
        var emptyOutfits = await _context.Outfits
            .Where(o => !o.OutfitClothes.Any())
            .ToListAsync();

        if (emptyOutfits.Count == 0)
            return;

        _context.Outfits.RemoveRange(emptyOutfits);
        await _context.SaveChangesAsync();
        Log.Information("Deleted empty outfits after clothing removal. Count={Count}", emptyOutfits.Count);
    }

    public async Task<IEnumerable<Outfit>> GetOutfitsByClothingIdAsync(Guid clothingId)
    {
        return await _context.Outfits
            .Include(o => o.OutfitClothes)
            .Where(o => o.OutfitClothes.Any(oc => oc.ClothingId == clothingId))
            .ToListAsync();
    }

    public async Task<List<OutfitUpdateResult>> DeleteInvalidOutfitsAsync(Guid excludedClothingId)
    {
        var results = new List<OutfitUpdateResult>();
        
        var outfitsWithClothing = await _context.Outfits
            .Include(o => o.OutfitClothes)
            .Where(o => o.OutfitClothes.Any(oc => oc.ClothingId == excludedClothingId))
            .ToListAsync();

        foreach (var outfit in outfitsWithClothing)
        {
            var link = outfit.OutfitClothes.FirstOrDefault(oc => oc.ClothingId == excludedClothingId);
            if (link != null)
            {
                outfit.OutfitClothes.Remove(link);
            }

            var remainingCount = outfit.OutfitClothes.Count;
            
            if (remainingCount < 2)
            {
                results.Add(new OutfitUpdateResult
                {
                    OutfitId = outfit.Id,
                    OutfitName = outfit.Name,
                    RemainingClothingCount = remainingCount,
                    WasDeleted = true
                });
                _context.Outfits.Remove(outfit);
            }
            else
            {
                results.Add(new OutfitUpdateResult
                {
                    OutfitId = outfit.Id,
                    OutfitName = outfit.Name,
                    RemainingClothingCount = remainingCount,
                    WasDeleted = false
                });
            }
        }

        await _context.SaveChangesAsync();
        return results;
    }

    public async Task<IEnumerable<Outfit>> GetBySceneAsync(OutfitScene scene)
    {
        return await _context.Outfits
            .AsNoTracking()
            .Where(o => o.Scene == scene)
            .Include(o => o.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .ThenInclude(c => c.ClothingTags)
            .ThenInclude(ct => ct.Tag)
            .Include(o => o.Favorites)
            .ToListAsync();
    }

    public async Task<IEnumerable<Outfit>> GetBySeasonAsync(Season season)
    {
        return await _context.Outfits
            .AsNoTracking()
            .Where(o => o.Season == season)
            .Include(o => o.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .ThenInclude(c => c.ClothingTags)
            .ThenInclude(ct => ct.Tag)
            .Include(o => o.Favorites)
            .ToListAsync();
    }

    public async Task<IEnumerable<Outfit>> GetRecentlyWornAsync(int count)
    {
        return await _context.Outfits
            .AsNoTracking()
            .Where(o => o.WornDate != null)
            .OrderByDescending(o => o.WornDate)
            .Take(count)
            .Include(o => o.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .ThenInclude(c => c.ClothingTags)
            .ThenInclude(ct => ct.Tag)
            .Include(o => o.Favorites)
            .ToListAsync();
    }
}
