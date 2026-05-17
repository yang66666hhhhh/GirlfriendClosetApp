using System.IO;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Data;
using ClosetApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Png;
using Xunit;

namespace ClosetApp.Tests;

public class ImageMaintenanceServiceTests
{
    [Fact]
    public async Task RelinkMissingImagesAsync_ReplacesBrokenImagePathWhenMatchingFileExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        var dbPath = Path.Combine(tempDir, "closet.db");
        var importDir = Path.Combine(tempDir, "import");
        var storageDir = Path.Combine(tempDir, "storage");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(importDir);
        Directory.CreateDirectory(storageDir);

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            await using (var context = new ClosetDbContext(options))
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                context.Clothes.Add(new Clothing
                {
                    Name = "Missing Coat",
                    Type = ClothingType.Outerwear,
                    Season = Season.Winter,
                    ImagePath = "missing-coat.png"
                });
                await context.SaveChangesAsync();
            }

            var sourceImage = Path.Combine(importDir, "missing-coat.png");
            await CreatePngAsync(sourceImage);

            var storageService = new ImageStorageService(storageDir);
            var resolver = new ImageAssetResolver(storageService);
            var service = new ImageMaintenanceService(new TestDbContextFactory(options), resolver, storageService);

            var missingBefore = await service.CountMissingImagesAsync();
            var repairedCount = await service.RelinkMissingImagesAsync(importDir);
            var missingAfter = await service.CountMissingImagesAsync();

            Assert.Equal(1, missingBefore);
            Assert.Equal(1, repairedCount);
            Assert.Equal(0, missingAfter);

            await using var assertContext = new ClosetDbContext(options);
            var clothing = Assert.Single(await assertContext.Clothes.ToListAsync());
            Assert.NotEqual("missing-coat.png", clothing.ImagePath);
            Assert.True(File.Exists(storageService.GetImageFullPath(clothing.ImagePath!)));
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
            }
        }
    }

    private static async Task CreatePngAsync(string path)
    {
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(64, 64);
        await using var stream = File.OpenWrite(path);
        await image.SaveAsync(stream, new PngEncoder());
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
