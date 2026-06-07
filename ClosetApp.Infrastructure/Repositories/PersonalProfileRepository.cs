using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;
using ClosetApp.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClosetApp.Infrastructure.Repositories;

public sealed class PersonalProfileRepository : IPersonalProfileRepository
{
    private readonly Data.ClosetDbContext _context;
    private readonly ICurrentUserContext? _currentUserContext;

    public PersonalProfileRepository(Data.ClosetDbContext context, ICurrentUserContext? currentUserContext = null)
    {
        _context = context;
        _currentUserContext = currentUserContext;
    }

    public async Task<IEnumerable<PersonalProfile>> GetAllAsync()
    {
        var query = await ForCurrentUserAsync(_context.PersonalProfiles);
        return await query
            .AsNoTracking()
            .OrderBy(profile => profile.CreatedAt)
            .ToListAsync();
    }

    public async Task<PersonalProfile?> GetByIdAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.PersonalProfiles);
        return await query
            .AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.Id == id);
    }

    public async Task<PersonalProfile?> GetCurrentAsync()
    {
        var query = await ForCurrentUserAsync(_context.PersonalProfiles);
        return await query
            .AsNoTracking()
            .OrderBy(profile => profile.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(PersonalProfile entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.PersonalProfiles.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PersonalProfile entity)
    {
        await AssignCurrentUserAsync(entity);
        _context.PersonalProfiles.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var query = await ForCurrentUserAsync(_context.PersonalProfiles);
        var profile = await query.FirstOrDefaultAsync(item => item.Id == id);
        if (profile == null)
            return;

        _context.PersonalProfiles.Remove(profile);
        await _context.SaveChangesAsync();
    }

    private async Task<IQueryable<PersonalProfile>> ForCurrentUserAsync(IQueryable<PersonalProfile> query)
    {
        if (_currentUserContext == null)
            return query;

        var userId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
        return query.Where(profile => profile.LocalUserId == userId);
    }

    private async Task AssignCurrentUserAsync(PersonalProfile entity)
    {
        if (_currentUserContext == null || entity.LocalUserId.HasValue)
            return;

        entity.LocalUserId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
    }
}
