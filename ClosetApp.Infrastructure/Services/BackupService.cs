using System.Text.Json;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClosetApp.Infrastructure.Services;

public sealed class BackupService : IBackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IDbContextFactory<ClosetDbContext> _dbContextFactory;

    public BackupService(IDbContextFactory<ClosetDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task ExportAsync(string filePath)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var document = new ClosetBackupDocument
        {
            Tags = await context.Tags
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync(),
            Clothes = await context.Clothes
                .AsNoTracking()
                .Include(c => c.ClothingTags)
                .Select(c => new ClothingBackupItem
                {
                    Id = c.Id,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    Name = c.Name,
                    Type = c.Type,
                    GarmentType = c.GarmentType,
                    ImagePath = c.ImagePath,
                    Color = c.Color,
                    Brand = c.Brand,
                    Notes = c.Notes,
                    Season = c.Season,
                    FavoriteLevel = c.FavoriteLevel,
                    IsFavorite = c.IsFavorite,
                    TagIds = c.ClothingTags.Select(ct => ct.TagId).ToList()
                })
                .OrderBy(c => c.Name)
                .ToListAsync(),
            Outfits = await context.Outfits
                .AsNoTracking()
                .Include(o => o.OutfitClothes)
                .Select(o => new OutfitBackupItem
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt,
                    Name = o.Name,
                    Scene = o.Scene,
                    Season = o.Season,
                    Rating = o.Rating,
                    Notes = o.Notes,
                    WornDate = o.WornDate,
                    WearCount = o.WearCount,
                    ClothingIds = o.OutfitClothes.Select(oc => oc.ClothingId).ToList()
                })
                .OrderBy(o => o.Name)
                .ToListAsync(),
            WornRecords = await context.OutfitWornRecords
                .AsNoTracking()
                .OrderBy(r => r.WornDate)
                .ToListAsync(),
            Favorites = await context.Favorites
                .AsNoTracking()
                .OrderBy(f => f.CreatedAt)
                .ToListAsync()
        };

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(document, JsonOptions));
    }

    public async Task ImportAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var document = JsonSerializer.Deserialize<ClosetBackupDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("备份文件格式无效。");

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        await ClearExistingDataAsync(context);

        context.Tags.AddRange(document.Tags);

        var clothes = document.Clothes.Select(item =>
        {
            var clothing = new Clothing
            {
                Id = item.Id,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                Name = item.Name,
                Type = item.Type,
                GarmentType = item.GarmentType,
                ImagePath = item.ImagePath,
                Color = item.Color,
                Brand = item.Brand,
                Notes = item.Notes,
                Season = item.Season,
                FavoriteLevel = item.FavoriteLevel,
                IsFavorite = item.IsFavorite
            };

            clothing.ClothingTags = item.TagIds
                .Select(tagId => new ClothingTag { ClothingId = clothing.Id, TagId = tagId })
                .ToList();
            return clothing;
        }).ToList();
        context.Clothes.AddRange(clothes);

        var outfits = document.Outfits.Select(item =>
        {
            var outfit = new Outfit
            {
                Id = item.Id,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                Name = item.Name,
                Scene = item.Scene,
                Season = item.Season,
                Rating = item.Rating,
                Notes = item.Notes,
                WornDate = item.WornDate,
                WearCount = item.WearCount
            };

            outfit.OutfitClothes = item.ClothingIds
                .Select(clothingId => new OutfitClothing { OutfitId = outfit.Id, ClothingId = clothingId })
                .ToList();
            return outfit;
        }).ToList();
        context.Outfits.AddRange(outfits);

        context.OutfitWornRecords.AddRange(document.WornRecords);
        context.Favorites.AddRange(document.Favorites);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task ClearExistingDataAsync(ClosetDbContext context)
    {
        context.OutfitWornRecords.RemoveRange(await context.OutfitWornRecords.ToListAsync());
        context.Favorites.RemoveRange(await context.Favorites.ToListAsync());
        context.OutfitClothes.RemoveRange(await context.OutfitClothes.ToListAsync());
        context.ClothingTags.RemoveRange(await context.ClothingTags.ToListAsync());
        context.Outfits.RemoveRange(await context.Outfits.ToListAsync());
        context.Clothes.RemoveRange(await context.Clothes.ToListAsync());
        context.Tags.RemoveRange(await context.Tags.ToListAsync());
        await context.SaveChangesAsync();
    }
}
