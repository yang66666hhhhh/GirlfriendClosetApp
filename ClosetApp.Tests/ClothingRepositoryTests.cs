using System.IO;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Data;
using ClosetApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClosetApp.Tests;

public class ClothingRepositoryTests
{
    [Fact]
    public async Task AddRangeAsync_SavesClothesAndTagLinksInSingleCall()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "closet.db");

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var tagId = Guid.NewGuid();
            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();
                setupContext.Tags.Add(new Tag
                {
                    Id = tagId,
                    Name = "通勤",
                    Color = "#FFFFFF",
                    Category = TagCategory.Style
                });
                await setupContext.SaveChangesAsync();
            }

            await using (var importContext = new ClosetDbContext(options))
            {
                var repository = new ClothingRepository(importContext);
                await repository.AddRangeAsync([
                    CreateClothing("奶白短外套", tagId),
                    CreateClothing("黑色半裙", tagId)
                ]);
            }

            await using var assertContext = new ClosetDbContext(options);
            var clothes = await assertContext.Clothes
                .Include(clothing => clothing.ClothingTags)
                .OrderBy(clothing => clothing.Name)
                .ToListAsync();

            Assert.Equal(2, clothes.Count);
            Assert.Contains(clothes, clothing => clothing.Name == "奶白短外套");
            Assert.Contains(clothes, clothing => clothing.Name == "黑色半裙");
            Assert.All(clothes, clothing =>
            {
                var clothingTag = Assert.Single(clothing.ClothingTags);
                Assert.Equal(tagId, clothingTag.TagId);
            });
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task AddRangeAsync_WithEmptyCollection_DoesNotTouchDatabase()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "closet.db");

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            await using var context = new ClosetDbContext(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            var repository = new ClothingRepository(context);
            await repository.AddRangeAsync([]);

            Assert.Empty(await context.Clothes.ToListAsync());
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    private static Clothing CreateClothing(string name, Guid tagId)
    {
        return new Clothing
        {
            Name = name,
            Type = ClothingType.Outerwear,
            Season = Season.Winter,
            ClothingTags = [new ClothingTag { TagId = tagId }]
        };
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
