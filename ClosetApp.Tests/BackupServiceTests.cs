using System.IO;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Data;
using ClosetApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClosetApp.Tests;

public class BackupServiceTests
{
    [Fact]
    public async Task ExportAndImport_RoundTripsCoreData()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "closet.db");

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();

                var tag = new Tag { Name = "通勤", Color = "#FFFFFF", Category = TagCategory.Style };
                var clothing = new Clothing
                {
                    Name = "Grey Blazer",
                    Type = ClothingType.Outerwear,
                    Season = Season.Autumn,
                    FavoriteLevel = 4,
                    IsFavorite = true
                };
                clothing.ClothingTags.Add(new ClothingTag { ClothingId = clothing.Id, TagId = tag.Id, Tag = tag });

                var outfit = new Outfit
                {
                    Name = "Office Look",
                    Scene = OutfitScene.Work,
                    Season = Season.Autumn
                };
                outfit.OutfitClothes.Add(new OutfitClothing { OutfitId = outfit.Id, ClothingId = clothing.Id, Clothing = clothing });

                setupContext.Tags.Add(tag);
                setupContext.Clothes.Add(clothing);
                setupContext.Outfits.Add(outfit);
                setupContext.OutfitWornRecords.Add(new OutfitWornRecord { OutfitId = outfit.Id, WornDate = new DateTime(2026, 5, 17) });
                await setupContext.SaveChangesAsync();
            }

            var factory = new TestDbContextFactory(options);
            var service = new BackupService(factory);
            var backupPath = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", $"{Guid.NewGuid():N}.json");

            await service.ExportAsync(backupPath);

            await using (var resetContext = new ClosetDbContext(options))
            {
                resetContext.Clothes.RemoveRange(await resetContext.Clothes.ToListAsync());
                resetContext.Tags.RemoveRange(await resetContext.Tags.ToListAsync());
                resetContext.Outfits.RemoveRange(await resetContext.Outfits.ToListAsync());
                resetContext.OutfitWornRecords.RemoveRange(await resetContext.OutfitWornRecords.ToListAsync());
                await resetContext.SaveChangesAsync();
            }

            await service.ImportAsync(backupPath);

            await using var assertContext = new ClosetDbContext(options);
            Assert.Contains(await assertContext.Tags.ToListAsync(), tag => tag.Name == "通勤");

            var restoredClothing = Assert.Single(
                await assertContext.Clothes
                    .Include(c => c.ClothingTags)
                    .Where(c => c.Name == "Grey Blazer")
                    .ToListAsync());
            Assert.Single(restoredClothing.ClothingTags);

            var restoredOutfit = Assert.Single(
                await assertContext.Outfits
                    .Include(o => o.OutfitClothes)
                    .Where(o => o.Name == "Office Look")
                    .ToListAsync());
            Assert.Single(restoredOutfit.OutfitClothes);

            Assert.Contains(await assertContext.OutfitWornRecords.ToListAsync(), record => record.OutfitId == restoredOutfit.Id);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // SQLite may briefly hold the file handle after the test completes.
            }
        }
    }

    private sealed class TestDbContextFactory : IDbContextFactory<ClosetDbContext>
    {
        private readonly DbContextOptions<ClosetDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ClosetDbContext> options)
        {
            _options = options;
        }

        public ClosetDbContext CreateDbContext()
        {
            return new ClosetDbContext(_options);
        }

        public Task<ClosetDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}
