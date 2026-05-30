using System.IO;
using System.Text.Json;
using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Data;
using ClosetApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitRepositoryTests
{
    [Fact]
    public async Task DeleteInvalidOutfitsAsync_WithStaleCompleteSnapshot_RefreshesSnapshotBeforeRemovingClothing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "closet.db");

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var outfitId = Guid.NewGuid();
            var topId = Guid.NewGuid();
            var skirtId = Guid.NewGuid();
            var shoesId = Guid.NewGuid();

            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();

                var top = new Clothing { Id = topId, Name = "白衬衫", Type = ClothingType.Top, Season = Season.AllSeason };
                var skirt = new Clothing { Id = skirtId, Name = "黑色半裙", Type = ClothingType.Skirt, Season = Season.AllSeason };
                var shoes = new Clothing { Id = shoesId, Name = "小白鞋", Type = ClothingType.Shoes, Season = Season.AllSeason };
                var outfit = new Outfit
                {
                    Id = outfitId,
                    Name = "约会搭配",
                    Scene = OutfitScene.Date,
                    Season = Season.AllSeason,
                    OriginalClothingCount = 0
                };

                outfit.OutfitClothes.Add(new OutfitClothing { OutfitId = outfitId, ClothingId = topId, Clothing = top });
                outfit.OutfitClothes.Add(new OutfitClothing { OutfitId = outfitId, ClothingId = skirtId, Clothing = skirt });
                outfit.OutfitClothes.Add(new OutfitClothing { OutfitId = outfitId, ClothingId = shoesId, Clothing = shoes });

                setupContext.Outfits.Add(outfit);
                setupContext.OutfitWornRecords.Add(new OutfitWornRecord
                {
                    Id = Guid.NewGuid(),
                    OutfitId = outfitId,
                    OutfitNameSnapshot = "约会搭配",
                    ClothingCountSnapshot = 1,
                    ClothingDetailsSnapshot = JsonSerializer.Serialize(new[]
                    {
                        new ClothingSnapshotDto { Id = topId, Name = "白衬衫", Type = nameof(ClothingType.Top) }
                    }),
                    IsSnapshotComplete = true,
                    WornDate = new DateTime(2026, 5, 20, 9, 0, 0)
                });
                await setupContext.SaveChangesAsync();
            }

            await using (var deleteContext = new ClosetDbContext(options))
            {
                var repository = new OutfitRepository(deleteContext);
                await repository.DeleteInvalidOutfitsAsync(skirtId);
            }

            await using var assertContext = new ClosetDbContext(options);
            var record = Assert.Single(await assertContext.OutfitWornRecords.ToListAsync());
            var outfitAfterDelete = await assertContext.Outfits
                .Include(outfit => outfit.OutfitClothes)
                .SingleAsync(outfit => outfit.Id == outfitId);
            var snapshotClothes = JsonSerializer.Deserialize<List<ClothingSnapshotDto>>(record.ClothingDetailsSnapshot!);

            Assert.Equal(3, record.ClothingCountSnapshot);
            Assert.True(record.IsSnapshotComplete);
            Assert.Contains(snapshotClothes!, clothing => clothing.Id == skirtId && clothing.Type == nameof(ClothingType.Skirt));
            Assert.Equal(3, outfitAfterDelete.OriginalClothingCount);
            Assert.DoesNotContain(outfitAfterDelete.OutfitClothes, link => link.ClothingId == skirtId);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch
        {
        }
    }
}
