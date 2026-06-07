using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClosetApp.Infrastructure.Repositories;

public sealed class LocalUserRepository : ILocalUserRepository
{
    private readonly Data.ClosetDbContext _context;

    public LocalUserRepository(Data.ClosetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LocalUser>> GetAllAsync()
    {
        return await _context.LocalUsers
            .AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.Role == LocalUserRole.SuperAdmin ? 0 : 1)
            .ThenBy(user => user.CreatedAt)
            .ToListAsync();
    }

    public async Task<LocalUser?> GetByIdAsync(Guid id)
    {
        return await _context.LocalUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id);
    }

    public async Task<LocalUser?> GetSuperAdminAsync()
    {
        return await _context.LocalUsers
            .AsNoTracking()
            .Where(user => user.IsActive && user.Role == LocalUserRole.SuperAdmin)
            .OrderBy(user => user.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<LocalUser?> GetActiveByIdAsync(Guid id)
    {
        return await _context.LocalUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id && user.IsActive);
    }

    public async Task<LocalUser?> GetActiveByAccountNameAsync(string accountName)
    {
        return await _context.LocalUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.AccountName == accountName && user.IsActive);
    }

    public async Task<int> CountActiveAsync()
    {
        return await _context.LocalUsers.CountAsync(user => user.IsActive);
    }

    public async Task AddAsync(LocalUser entity)
    {
        _context.LocalUsers.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(LocalUser entity)
    {
        var tracked = _context.LocalUsers.Local.FirstOrDefault(user => user.Id == entity.Id);
        if (tracked != null && !ReferenceEquals(tracked, entity))
            _context.Entry(tracked).State = EntityState.Detached;

        _context.LocalUsers.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await DeleteWorkspaceAsync(id);
    }

    public async Task AssignUnownedWorkspaceAsync(Guid userId)
    {
        foreach (var item in await _context.Clothes.Where(item => item.LocalUserId == null).ToListAsync())
            item.LocalUserId = userId;
        foreach (var item in await _context.Outfits.Where(item => item.LocalUserId == null).ToListAsync())
            item.LocalUserId = userId;
        foreach (var item in await _context.Tags.Where(item => item.LocalUserId == null).ToListAsync())
            item.LocalUserId = userId;
        foreach (var item in await _context.Favorites.Where(item => item.LocalUserId == null).ToListAsync())
            item.LocalUserId = userId;
        foreach (var item in await _context.OutfitWornRecords.Where(item => item.LocalUserId == null).ToListAsync())
            item.LocalUserId = userId;
        foreach (var item in await _context.PersonalProfiles.Where(item => item.LocalUserId == null).ToListAsync())
            item.LocalUserId = userId;
        foreach (var item in await _context.OutfitGeneratedImages.Where(item => item.LocalUserId == null).ToListAsync())
            item.LocalUserId = userId;

        await _context.SaveChangesAsync();
    }

    public async Task EnsureDefaultTagsAsync(Guid userId)
    {
        if (await _context.Tags.AnyAsync(tag => tag.LocalUserId == userId))
            return;

        var now = DateTime.Now;
        _context.Tags.AddRange(DefaultTags().Select(tag => new Tag
        {
            Id = Guid.NewGuid(),
            LocalUserId = userId,
            Name = tag.Name,
            Color = tag.Color,
            Category = tag.Category,
            CreatedAt = now,
            UpdatedAt = now
        }));

        await _context.SaveChangesAsync();
    }

    private static IEnumerable<(string Name, string Color, TagCategory Category)> DefaultTags()
    {
        yield return ("韩系", "#D8B7A3", TagCategory.Style);
        yield return ("极简", "#C8C2B8", TagCategory.Style);
        yield return ("通勤", "#B7C4B2", TagCategory.Style);
        yield return ("甜妹", "#E7C7C0", TagCategory.Style);
        yield return ("美式", "#C89B7B", TagCategory.Style);
        yield return ("复古", "#C4A98F", TagCategory.Style);
        yield return ("Clean Fit", "#A8B5A0", TagCategory.Style);
        yield return ("Y2K", "#D4A5C9", TagCategory.Style);
        yield return ("约会", "#E88D8D", TagCategory.Scene);
        yield return ("上班", "#8BA8D9", TagCategory.Scene);
        yield return ("出游", "#A8D4A8", TagCategory.Scene);
        yield return ("派对", "#D4A5E8", TagCategory.Scene);
    }

    public async Task DeleteWorkspaceAsync(Guid userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.OutfitGeneratedImages.RemoveRange(await _context.OutfitGeneratedImages.Where(image => image.LocalUserId == userId).ToListAsync());
        _context.OutfitWornRecords.RemoveRange(await _context.OutfitWornRecords.Where(record => record.LocalUserId == userId).ToListAsync());
        _context.Favorites.RemoveRange(await _context.Favorites.Where(favorite => favorite.LocalUserId == userId).ToListAsync());
        _context.PersonalProfiles.RemoveRange(await _context.PersonalProfiles.Where(profile => profile.LocalUserId == userId).ToListAsync());
        _context.Outfits.RemoveRange(await _context.Outfits.Where(outfit => outfit.LocalUserId == userId).ToListAsync());
        _context.Clothes.RemoveRange(await _context.Clothes.Where(clothing => clothing.LocalUserId == userId).ToListAsync());
        _context.Tags.RemoveRange(await _context.Tags.Where(tag => tag.LocalUserId == userId).ToListAsync());

        var user = await _context.LocalUsers.FindAsync(userId);
        if (user != null)
            _context.LocalUsers.Remove(user);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
