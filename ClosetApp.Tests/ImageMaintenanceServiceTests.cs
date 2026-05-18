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
    public async Task CountMissingThumbnailsAsync_OnlyCountsStoredImagesWithoutThumbnail()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        var dbPath = Path.Combine(tempDir, "closet.db");
        var sourceDir = Path.Combine(tempDir, "source");
        var storageDir = Path.Combine(tempDir, "storage");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(storageDir);

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var storageService = new ImageStorageService(storageDir);
            var firstSourceImage = Path.Combine(sourceDir, "coat-a.png");
            var secondSourceImage = Path.Combine(sourceDir, "coat-b.png");
            await CreatePngAsync(firstSourceImage);
            await CreatePngAsync(secondSourceImage);

            var storedWithMissingThumbnail = await storageService.SaveImageAsync(firstSourceImage);
            var storedWithThumbnail = await storageService.SaveImageAsync(secondSourceImage);
            File.Delete(storageService.GetThumbnailFullPath(storedWithMissingThumbnail));

            await using (var context = new ClosetDbContext(options))
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                context.Clothes.AddRange(
                    CreateClothing("Missing Thumb", storedWithMissingThumbnail),
                    CreateClothing("Healthy Thumb", storedWithThumbnail),
                    CreateClothing("Broken Source", "broken-source.png"),
                    CreateClothing("Duplicate Thumb", storedWithMissingThumbnail));
                await context.SaveChangesAsync();
            }

            var resolver = new ImageAssetResolver(storageService);
            var service = new ImageMaintenanceService(new TestDbContextFactory(options), resolver, storageService);

            var missingThumbnailCount = await service.CountMissingThumbnailsAsync();

            Assert.Equal(1, missingThumbnailCount);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task RebuildMissingThumbnailsAsync_RebuildsMissingThumbnailsAndReturnsSummary()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        var dbPath = Path.Combine(tempDir, "closet.db");
        var sourceDir = Path.Combine(tempDir, "source");
        var storageDir = Path.Combine(tempDir, "storage");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(storageDir);

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var storageService = new ImageStorageService(storageDir);
            var firstSourceImage = Path.Combine(sourceDir, "dress-a.png");
            var secondSourceImage = Path.Combine(sourceDir, "dress-b.png");
            await CreatePngAsync(firstSourceImage);
            await CreatePngAsync(secondSourceImage);

            var storedWithMissingThumbnail = await storageService.SaveImageAsync(firstSourceImage);
            var storedWithThumbnail = await storageService.SaveImageAsync(secondSourceImage);
            var missingThumbnailPath = storageService.GetThumbnailFullPath(storedWithMissingThumbnail);
            File.Delete(missingThumbnailPath);

            await using (var context = new ClosetDbContext(options))
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                context.Clothes.AddRange(
                    CreateClothing("Rebuild Me", storedWithMissingThumbnail),
                    CreateClothing("Already Ready", storedWithThumbnail),
                    CreateClothing("Source Missing", "missing-image.png"),
                    CreateClothing("Duplicate Entry", storedWithMissingThumbnail));
                await context.SaveChangesAsync();
            }

            var resolver = new ImageAssetResolver(storageService);
            var service = new ImageMaintenanceService(new TestDbContextFactory(options), resolver, storageService);

            var missingBefore = await service.CountMissingThumbnailsAsync();
            var result = await service.RebuildMissingThumbnailsAsync(maxSize: 140);
            var missingAfter = await service.CountMissingThumbnailsAsync();

            Assert.Equal(1, missingBefore);
            Assert.Equal(3, result.ScannedImageCount);
            Assert.Equal(1, result.MissingThumbnailCount);
            Assert.Equal(1, result.RebuiltCount);
            Assert.Equal(1, result.SkippedCount);
            Assert.Equal(1, result.MissingSourceCount);
            Assert.Contains("重建 1 组图片缓存", result.Summary);
            Assert.Equal(0, missingAfter);
            Assert.True(File.Exists(missingThumbnailPath));
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

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
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task AnalyzeOrphanOriginalsAsync_CountsOnlyUnreferencedOriginals()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        var dbPath = Path.Combine(tempDir, "closet.db");
        var sourceDir = Path.Combine(tempDir, "source");
        var storageDir = Path.Combine(tempDir, "storage");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(storageDir);

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var storageService = new ImageStorageService(storageDir);
            var referencedSource = Path.Combine(sourceDir, "referenced.png");
            var orphanSource = Path.Combine(sourceDir, "orphan.png");
            await CreatePngAsync(referencedSource);
            await CreatePngAsync(orphanSource);

            var referencedFileName = await storageService.SaveImageAsync(referencedSource);
            await storageService.SaveImageAsync(orphanSource);

            await using (var context = new ClosetDbContext(options))
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                context.Clothes.Add(CreateClothing("Referenced", referencedFileName));
                await context.SaveChangesAsync();
            }

            var resolver = new ImageAssetResolver(storageService);
            var service = new ImageMaintenanceService(new TestDbContextFactory(options), resolver, storageService);

            var result = await service.AnalyzeOrphanOriginalsAsync();

            Assert.Equal(1, result.OrphanCount);
            Assert.True(result.TotalBytes > 0);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task CleanupOrphanOriginalsAsync_RemovesOriginalAndDerivedAssets()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        var dbPath = Path.Combine(tempDir, "closet.db");
        var sourceDir = Path.Combine(tempDir, "source");
        var storageDir = Path.Combine(tempDir, "storage");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(storageDir);

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var storageService = new ImageStorageService(storageDir);
            var referencedSource = Path.Combine(sourceDir, "referenced.png");
            var orphanSource = Path.Combine(sourceDir, "orphan.png");
            await CreatePngAsync(referencedSource);
            await CreatePngAsync(orphanSource);

            var referencedFileName = await storageService.SaveImageAsync(referencedSource);
            var orphanFileName = await storageService.SaveImageAsync(orphanSource);
            var orphanOriginalPath = storageService.GetImageFullPath(orphanFileName);
            var orphanDisplayPath = storageService.GetDisplayFullPath(orphanFileName);
            var orphanThumbnailPath = storageService.GetThumbnailFullPath(orphanFileName);

            await using (var context = new ClosetDbContext(options))
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                context.Clothes.Add(CreateClothing("Referenced", referencedFileName));
                await context.SaveChangesAsync();
            }

            var resolver = new ImageAssetResolver(storageService);
            var service = new ImageMaintenanceService(new TestDbContextFactory(options), resolver, storageService);

            var result = await service.CleanupOrphanOriginalsAsync();

            Assert.Equal(1, result.DeletedOriginalCount);
            Assert.Equal(2, result.DeletedDerivedAssetCount);
            Assert.True(result.FreedBytes > 0);
            Assert.False(File.Exists(orphanOriginalPath));
            Assert.False(File.Exists(orphanDisplayPath));
            Assert.False(File.Exists(orphanThumbnailPath));
            Assert.True(File.Exists(storageService.GetImageFullPath(referencedFileName)));
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    private static async Task CreatePngAsync(string path)
    {
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(64, 64);
        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        await File.WriteAllBytesAsync(path, stream.ToArray());
    }

    private static Clothing CreateClothing(string name, string imagePath)
    {
        return new Clothing
        {
            Name = name,
            Type = ClothingType.Outerwear,
            Season = Season.Winter,
            ImagePath = imagePath
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
