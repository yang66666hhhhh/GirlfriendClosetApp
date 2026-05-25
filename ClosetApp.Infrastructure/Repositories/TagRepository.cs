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
        // 标签页需要知道每个标签被多少件衣物使用，因此这里一并带上关联。
        return await _context.Tags
            .Include(tag => tag.ClothingTags)
            .ToListAsync();
    }

    public async Task<Tag?> GetByIdAsync(Guid id)
    {
        return await _context.Tags.FindAsync(id);
    }

    public async Task<IEnumerable<Tag>> GetByCategoryAsync(TagCategory category)
    {
        return await _context.Tags
            .Include(tag => tag.ClothingTags)
            .Where(tag => tag.Category == category)
            .ToListAsync();
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
