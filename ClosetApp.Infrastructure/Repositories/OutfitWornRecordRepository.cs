using Microsoft.EntityFrameworkCore;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;

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
        return await _context.OutfitWornRecords.ToListAsync();
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
            .Where(r => r.WornDate >= start && r.WornDate <= end)
            .ToListAsync();
    }

    public async Task<IEnumerable<OutfitWornRecord>> GetByOutfitIdAsync(Guid outfitId)
    {
        return await _context.OutfitWornRecords
            .Where(r => r.OutfitId == outfitId)
            .ToListAsync();
    }
}