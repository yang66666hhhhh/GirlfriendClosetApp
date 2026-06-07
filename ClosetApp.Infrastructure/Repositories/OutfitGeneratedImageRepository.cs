using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;
using ClosetApp.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClosetApp.Infrastructure.Repositories;

public sealed class OutfitGeneratedImageRepository : IOutfitGeneratedImageRepository
{
    private readonly Data.ClosetDbContext _context;
    private readonly ICurrentUserContext? _currentUserContext;

    public OutfitGeneratedImageRepository(Data.ClosetDbContext context, ICurrentUserContext? currentUserContext = null)
    {
        _context = context;
        _currentUserContext = currentUserContext;
    }

    public async Task<IEnumerable<OutfitGeneratedImage>> GetAllAsync()
    {
        var query = await ForCurrentUserAsync(_context.OutfitGeneratedImages);
        return await query
            .AsNoTracking()
            .OrderByDescending(image => image.CreatedAt)
            .ToListAsync();
    }

    public async Task<OutfitGeneratedImage?> GetByIdAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.OutfitGeneratedImages);
        return await query
            .AsNoTracking()
            .FirstOrDefaultAsync(image => image.Id == id);
    }

    public async Task<IReadOnlyList<OutfitGeneratedImage>> GetByOutfitIdAsync(Guid outfitId)
    {
        var query = await ForCurrentUserAsync(_context.OutfitGeneratedImages);
        return await query
            .AsNoTracking()
            .Where(image => image.OutfitId == outfitId)
            .OrderByDescending(image => image.CreatedAt)
            .ToListAsync();
    }

    public async Task<OutfitGeneratedImage?> GetPrimaryByOutfitIdAsync(Guid outfitId)
    {
        var query = await ForCurrentUserAsync(_context.OutfitGeneratedImages);
        return await query
            .AsNoTracking()
            .FirstOrDefaultAsync(image => image.OutfitId == outfitId && image.IsPrimary);
    }

    public async Task ClearPrimaryAsync(Guid outfitId, Guid? excludingId = null)
    {
        var query = await ForCurrentUserAsync(_context.OutfitGeneratedImages);
        var images = await query
            .Where(image => image.OutfitId == outfitId && image.IsPrimary)
            .ToListAsync();

        foreach (var image in images)
        {
            if (excludingId.HasValue && image.Id == excludingId.Value)
                continue;

            image.IsPrimary = false;
            image.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task AddAsync(OutfitGeneratedImage entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.OutfitGeneratedImages.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OutfitGeneratedImage entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.OutfitGeneratedImages.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.OutfitGeneratedImages);
        var image = await query.FirstOrDefaultAsync(item => item.Id == id);
        if (image == null)
            return;

        _context.OutfitGeneratedImages.Remove(image);
        await _context.SaveChangesAsync();
    }

    private async Task<IQueryable<OutfitGeneratedImage>> ForCurrentUserAsync(IQueryable<OutfitGeneratedImage> query)
    {
        if (_currentUserContext == null)
            return query;

        var userId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
        return query.Where(image => image.LocalUserId == userId);
    }

    private async Task AssignCurrentUserAsync(OutfitGeneratedImage entity)
    {
        if (_currentUserContext == null || entity.LocalUserId.HasValue)
            return;

        entity.LocalUserId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
    }
}
