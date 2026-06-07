using Microsoft.EntityFrameworkCore;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
    private readonly Data.ClosetDbContext _context;
    private readonly ICurrentUserContext? _currentUserContext;

    public TagRepository(Data.ClosetDbContext context, ICurrentUserContext? currentUserContext = null)
    {
        _context = context;
        _currentUserContext = currentUserContext;
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        // 标签页需要知道每个标签被多少件衣物使用，因此这里一并带上关联。
        var query = await ForCurrentUserAsync(_context.Tags);
        return await query
            .Include(tag => tag.ClothingTags)
            .ToListAsync();
    }

    public async Task<Tag?> GetByIdAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.Tags);
        return await query.FirstOrDefaultAsync(tag => tag.Id == id);
    }

    public async Task<IEnumerable<Tag>> GetByCategoryAsync(TagCategory category)
    {
        var query = await ForCurrentUserAsync(_context.Tags);
        return await query
            .Include(tag => tag.ClothingTags)
            .Where(tag => tag.Category == category)
            .ToListAsync();
    }

    public async Task AddAsync(Tag entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.Tags.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tag entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.Tags.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.Tags);
        var tag = await query.FirstOrDefaultAsync(item => item.Id == id);
        if (tag != null)
        {
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
        }
    }

    private async Task<IQueryable<Tag>> ForCurrentUserAsync(IQueryable<Tag> query)
    {
        if (_currentUserContext == null)
            return query;

        var userId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
        return query.Where(tag => tag.LocalUserId == userId);
    }

    private async Task AssignCurrentUserAsync(Tag entity)
    {
        if (_currentUserContext == null || entity.LocalUserId.HasValue)
            return;

        entity.LocalUserId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
    }
}
