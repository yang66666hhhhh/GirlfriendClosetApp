using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClosetApp.Infrastructure.Repositories;

public sealed class PersonalProfileRepository : IPersonalProfileRepository
{
    private readonly Data.ClosetDbContext _context;

    public PersonalProfileRepository(Data.ClosetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PersonalProfile>> GetAllAsync()
    {
        return await _context.PersonalProfiles
            .AsNoTracking()
            .OrderBy(profile => profile.CreatedAt)
            .ToListAsync();
    }

    public async Task<PersonalProfile?> GetByIdAsync(Guid id)
    {
        return await _context.PersonalProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.Id == id);
    }

    public async Task<PersonalProfile?> GetCurrentAsync()
    {
        return await _context.PersonalProfiles
            .AsNoTracking()
            .OrderBy(profile => profile.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(PersonalProfile entity)
    {
        _context.PersonalProfiles.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PersonalProfile entity)
    {
        _context.PersonalProfiles.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var profile = await _context.PersonalProfiles.FindAsync(id);
        if (profile == null)
            return;

        _context.PersonalProfiles.Remove(profile);
        await _context.SaveChangesAsync();
    }
}
