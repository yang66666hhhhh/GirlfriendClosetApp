using Microsoft.EntityFrameworkCore;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;
using System.IO;

namespace ClosetApp.Infrastructure.Repositories;

public class OutfitWornRecordRepository : IOutfitWornRecordRepository
{
    private readonly Data.ClosetDbContext _context;

    public OutfitWornRecordRepository(Data.ClosetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OutfitWornRecord>> GetAllAsync()
    {
        return await _context.OutfitWornRecords
            .Include(r => r.Outfit)
            .ThenInclude(o => o!.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .ToListAsync();
    }

    public async Task<OutfitWornRecord?> GetByIdAsync(Guid id)
    {
        return await _context.OutfitWornRecords.FindAsync(id);
    }

    public async Task AddAsync(OutfitWornRecord entity)
    {
        _context.OutfitWornRecords.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OutfitWornRecord entity)
    {
        _context.OutfitWornRecords.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var record = await _context.OutfitWornRecords.FindAsync(id);
        if (record != null)
        {
            _context.OutfitWornRecords.Remove(record);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<OutfitWornRecord>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.OutfitWornRecords
            .Include(r => r.Outfit)
            .ThenInclude(o => o!.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .Where(r => r.WornDate >= start && r.WornDate <= end)
            .OrderByDescending(r => r.WornDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<OutfitWornRecord>> GetByOutfitIdAsync(Guid outfitId)
    {
        return await _context.OutfitWornRecords
            .Include(r => r.Outfit)
            .ThenInclude(o => o!.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .Where(r => r.OutfitId == outfitId)
            .OrderByDescending(r => r.WornDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<OutfitWornRecord>> GetRecentAsync(int count)
    {
        return await _context.OutfitWornRecords
            .Include(r => r.Outfit)
            .ThenInclude(o => o!.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .OrderByDescending(r => r.WornDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<bool> IsImageReferencedBySnapshotAsync(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return false;

        var fileName = Path.GetFileName(imagePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return await _context.OutfitWornRecords
            .AsNoTracking()
            .Where(record => record.ClothingDetailsSnapshot != null)
            .AnyAsync(record => EF.Functions.Like(record.ClothingDetailsSnapshot!, $"%{fileName}%"));
    }
}
