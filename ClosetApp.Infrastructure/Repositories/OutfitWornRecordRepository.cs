using Microsoft.EntityFrameworkCore;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;
using System.IO;
using ClosetApp.Application.Interfaces;

namespace ClosetApp.Infrastructure.Repositories;

public class OutfitWornRecordRepository : IOutfitWornRecordRepository
{
    private readonly Data.ClosetDbContext _context;
    private readonly ICurrentUserContext? _currentUserContext;

    public OutfitWornRecordRepository(Data.ClosetDbContext context, ICurrentUserContext? currentUserContext = null)
    {
        _context = context;
        _currentUserContext = currentUserContext;
    }

    public async Task<IEnumerable<OutfitWornRecord>> GetAllAsync()
    {
        var query = await ForCurrentUserAsync(_context.OutfitWornRecords);
        return await query
            .Include(r => r.Outfit)
            .ThenInclude(o => o!.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .ToListAsync();
    }

    public async Task<OutfitWornRecord?> GetByIdAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.OutfitWornRecords);
        return await query.FirstOrDefaultAsync(record => record.Id == id);
    }

    public async Task AddAsync(OutfitWornRecord entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.OutfitWornRecords.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OutfitWornRecord entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.OutfitWornRecords.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.OutfitWornRecords);
        var record = await query.FirstOrDefaultAsync(item => item.Id == id);
        if (record != null)
        {
            _context.OutfitWornRecords.Remove(record);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<OutfitWornRecord>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        var query = await ForCurrentUserAsync(_context.OutfitWornRecords);
        return await query
            .Include(r => r.Outfit)
            .ThenInclude(o => o!.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .Where(r => r.WornDate >= start && r.WornDate <= end)
            .OrderByDescending(r => r.WornDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<OutfitWornRecord>> GetByOutfitIdAsync(Guid outfitId)
    {
        var query = await ForCurrentUserAsync(_context.OutfitWornRecords);
        return await query
            .Include(r => r.Outfit)
            .ThenInclude(o => o!.OutfitClothes)
            .ThenInclude(oc => oc.Clothing)
            .Where(r => r.OutfitId == outfitId)
            .OrderByDescending(r => r.WornDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<OutfitWornRecord>> GetRecentAsync(int count)
    {
        var query = await ForCurrentUserAsync(_context.OutfitWornRecords);
        return await query
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

        var query = await ForCurrentUserAsync(_context.OutfitWornRecords);
        return await query
            .AsNoTracking()
            .Where(record => record.ClothingDetailsSnapshot != null)
            .AnyAsync(record => EF.Functions.Like(record.ClothingDetailsSnapshot!, $"%{fileName}%"));
    }

    private async Task<IQueryable<OutfitWornRecord>> ForCurrentUserAsync(IQueryable<OutfitWornRecord> query)
    {
        if (_currentUserContext == null)
            return query;

        var userId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
        return query.Where(record => record.LocalUserId == userId);
    }

    private async Task AssignCurrentUserAsync(OutfitWornRecord entity)
    {
        if (_currentUserContext == null || entity.LocalUserId.HasValue)
            return;

        entity.LocalUserId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
    }
}
