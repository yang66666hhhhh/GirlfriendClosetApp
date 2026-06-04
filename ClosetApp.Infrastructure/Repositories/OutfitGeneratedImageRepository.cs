using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClosetApp.Infrastructure.Repositories;

public sealed class OutfitGeneratedImageRepository : IOutfitGeneratedImageRepository
{
    private readonly Data.ClosetDbContext _context;

    public OutfitGeneratedImageRepository(Data.ClosetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OutfitGeneratedImage>> GetAllAsync()
    {
        return await _context.OutfitGeneratedImages
            .AsNoTracking()
            .OrderByDescending(image => image.CreatedAt)
            .ToListAsync();
    }

    public async Task<OutfitGeneratedImage?> GetByIdAsync(Guid id)
    {
        return await _context.OutfitGeneratedImages
            .AsNoTracking()
            .FirstOrDefaultAsync(image => image.Id == id);
    }

    public async Task<IReadOnlyList<OutfitGeneratedImage>> GetByOutfitIdAsync(Guid outfitId)
    {
        return await _context.OutfitGeneratedImages
            .AsNoTracking()
            .Where(image => image.OutfitId == outfitId)
            .OrderByDescending(image => image.CreatedAt)
            .ToListAsync();
    }

    public async Task<OutfitGeneratedImage?> GetPrimaryByOutfitIdAsync(Guid outfitId)
    {
        return await _context.OutfitGeneratedImages
            .AsNoTracking()
            .FirstOrDefaultAsync(image => image.OutfitId == outfitId && image.IsPrimary);
    }

    public async Task ClearPrimaryAsync(Guid outfitId, Guid? excludingId = null)
    {
        var images = await _context.OutfitGeneratedImages
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
        _context.OutfitGeneratedImages.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OutfitGeneratedImage entity)
    {
        _context.OutfitGeneratedImages.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var image = await _context.OutfitGeneratedImages.FindAsync(id);
        if (image == null)
            return;

        _context.OutfitGeneratedImages.Remove(image);
        await _context.SaveChangesAsync();
    }
}
