using Microsoft.EntityFrameworkCore;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
    private readonly Data.ClosetDbContext _context;

    public TagRepository(Data.ClosetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        return await _context.Tags.ToListAsync();
    }

    public async Task<Tag?> GetByIdAsync(Guid id)
    {
        return await _context.Tags.FindAsync(id);
    }

    public async Task<IEnumerable<Tag>> GetByCategoryAsync(TagCategory category)
    {
        return await _context.Tags.Where(t => t.Category == category).ToListAsync();
    }

    public async Task AddAsync(Tag entity)
    {
        _context.Tags.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tag entity)
    {
        _context.Tags.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag != null)
        {
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
        }
    }
}