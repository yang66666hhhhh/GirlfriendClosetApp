using System.IO;
using System.IO.Compression;
using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Data;
using ClosetApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ClosetApp.Tests;

public class BackupServiceTests
{
    [Fact]
    public async Task ExportAndImport_Json_RoundTripsCoreData()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "closet.db");
        var historyDir = Path.Combine(tempDir, "history");

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
            var service = new BackupService(factory, historyDirectory: historyDir);
            var backupPath = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", $"{Guid.NewGuid():N}.json");

            var exportResult = await service.ExportAsync(backupPath);

            await using (var resetContext = new ClosetDbContext(options))
            {
                resetContext.Clothes.RemoveRange(await resetContext.Clothes.ToListAsync());
                resetContext.Tags.RemoveRange(await resetContext.Tags.ToListAsync());
                resetContext.Outfits.RemoveRange(await resetContext.Outfits.ToListAsync());
                resetContext.OutfitWornRecords.RemoveRange(await resetContext.OutfitWornRecords.ToListAsync());
                await resetContext.SaveChangesAsync();
            }

            var importResult = await service.ImportAsync(backupPath);

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
            Assert.Equal("json", exportResult.Format);
            Assert.Equal("json", importResult.Format);
            Assert.Empty(importResult.Warnings);
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

    [Fact]
    public async Task ExportAndImport_Zip_RestoresCoreDataAndImages()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "closet.db");
        var storageDir = Path.Combine(tempDir, "storage");
        var historyDir = Path.Combine(tempDir, "history");

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            var imageStorage = new ImageStorageService(storageDir);
            var sourceImagePath = Path.Combine(tempDir, "source.png");
            await CreateSourceImageAsync(sourceImagePath);
            var storedFileName = await imageStorage.SaveImageAsync(sourceImagePath);

            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();

                var clothing = new Clothing
                {
                    Name = "Pink Coat",
                    Type = ClothingType.Outerwear,
                    Season = Season.Winter,
                    ImagePath = storedFileName
                };

                setupContext.Clothes.Add(clothing);
                await setupContext.SaveChangesAsync();
            }

            var factory = new TestDbContextFactory(options);
            var service = new BackupService(factory, imageStorage, historyDir);
            var backupPath = Path.Combine(tempDir, "closet-backup.zip");

            var exportResult = await service.ExportAsync(backupPath);

            using (var archive = ZipFile.OpenRead(backupPath))
            {
                Assert.NotNull(archive.GetEntry("backup.json"));
                Assert.NotNull(archive.GetEntry($"images/{storedFileName}"));
            }

            await using (var resetContext = new ClosetDbContext(options))
            {
                resetContext.Clothes.RemoveRange(await resetContext.Clothes.ToListAsync());
                await resetContext.SaveChangesAsync();
            }

            await imageStorage.DeleteImageWithThumbnailAsync(storedFileName);
            Assert.False(File.Exists(imageStorage.GetImageFullPath(storedFileName)));
            Assert.False(File.Exists(imageStorage.GetThumbnailFullPath(storedFileName)));

            var importResult = await service.ImportAsync(backupPath);

            await using var assertContext = new ClosetDbContext(options);
            var restoredClothing = Assert.Single(await assertContext.Clothes.Where(c => c.Name == "Pink Coat").ToListAsync());
            Assert.Equal(storedFileName, restoredClothing.ImagePath);
            Assert.True(File.Exists(imageStorage.GetImageFullPath(storedFileName)));
            Assert.True(File.Exists(imageStorage.GetThumbnailFullPath(storedFileName)));

            Assert.Equal(1, exportResult.IncludedImageCount);
            Assert.Equal(1, importResult.RestoredImageCount);
            Assert.Empty(importResult.MissingImageFiles);

            var history = await service.GetHistoryAsync();
            Assert.Equal(2, history.Count);
            Assert.Equal("Import", history[0].Operation);
            Assert.Equal("Export", history[1].Operation);
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

    [Fact]
    public async Task ValidateExportAsync_WithJsonAndImages_ReturnsImageWarning()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "closet.db");
        var storageDir = Path.Combine(tempDir, "storage");

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var imageStorage = new ImageStorageService(storageDir);
            var sourceImagePath = Path.Combine(tempDir, "source.png");
            await CreateSourceImageAsync(sourceImagePath);
            var storedFileName = await imageStorage.SaveImageAsync(sourceImagePath);

            await using (var context = new ClosetDbContext(options))
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                context.Clothes.Add(new Clothing
                {
                    Name = "Navy Shirt",
                    Type = ClothingType.Top,
                    Season = Season.Spring,
                    ImagePath = storedFileName
                });
                await context.SaveChangesAsync();
            }

            var service = new BackupService(new TestDbContextFactory(options), imageStorage, Path.Combine(tempDir, "history"));
            var validation = await service.ValidateExportAsync(Path.Combine(tempDir, "backup.json"));

            Assert.True(validation.HasWarnings);
            Assert.Equal(1, validation.ReferencedImageCount);
            Assert.Equal(1, validation.IncludedImageCount);
            Assert.Equal(0, validation.MissingImageCount);
            Assert.Contains("JSON 仅导出核心数据", validation.ImageSummary, StringComparison.Ordinal);
            Assert.Contains(validation.Warnings, warning => warning.Contains("JSON 备份只保存核心数据", StringComparison.Ordinal));
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

    [Fact]
    public async Task ValidateExportAsync_WithMissingZipImage_ReturnsCoverageCounts()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "closet.db");
        var storageDir = Path.Combine(tempDir, "storage");

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var imageStorage = new ImageStorageService(storageDir);

            await using (var context = new ClosetDbContext(options))
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                context.Clothes.Add(new Clothing
                {
                    Name = "Broken Coat",
                    Type = ClothingType.Outerwear,
                    Season = Season.Winter,
                    ImagePath = "missing-coat.png"
                });
                await context.SaveChangesAsync();
            }

            var service = new BackupService(new TestDbContextFactory(options), imageStorage, Path.Combine(tempDir, "history"));
            var validation = await service.ValidateExportAsync(Path.Combine(tempDir, "backup.zip"));

            Assert.True(validation.HasWarnings);
            Assert.Equal(1, validation.ReferencedImageCount);
            Assert.Equal(0, validation.IncludedImageCount);
            Assert.Equal(1, validation.MissingImageCount);
            Assert.Contains("可打包 0 张", validation.ImageSummary, StringComparison.Ordinal);
            Assert.Contains("缺失 1 张", validation.ImageSummary, StringComparison.Ordinal);
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

    [Fact]
    public void BackupValidationResult_WithEmptyCounts_ReportsEmptyBackupReadiness()
    {
        var validation = new BackupValidationResult(
            "zip",
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            ["当前没有可导出的数据，这会生成一个空备份。"]);

        Assert.True(validation.IsEmptyBackup);
        Assert.True(validation.HasWarnings);
        Assert.Contains("空备份", validation.ReadinessSummary, StringComparison.Ordinal);
        Assert.Contains("没有关联图片", validation.ImageSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportAsync_ZipMissingImage_ReportsMissingFileNames()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "closet.db");
        var storageDir = Path.Combine(tempDir, "storage");
        var historyDir = Path.Combine(tempDir, "history");

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            var imageStorage = new ImageStorageService(storageDir);
            var sourceImagePath = Path.Combine(tempDir, "source.png");
            await CreateSourceImageAsync(sourceImagePath);
            var storedFileName = await imageStorage.SaveImageAsync(sourceImagePath);

            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();
                setupContext.Clothes.Add(new Clothing
                {
                    Name = "Missing Image Coat",
                    Type = ClothingType.Outerwear,
                    Season = Season.Winter,
                    ImagePath = storedFileName
                });
                await setupContext.SaveChangesAsync();
            }

            var service = new BackupService(new TestDbContextFactory(options), imageStorage, historyDir);
            var backupPath = Path.Combine(tempDir, "missing-image-backup.zip");
            await service.ExportAsync(backupPath);

            using (var archive = ZipFile.Open(backupPath, ZipArchiveMode.Update))
            {
                archive.GetEntry($"images/{storedFileName}")?.Delete();
            }

            await using (var resetContext = new ClosetDbContext(options))
            {
                resetContext.Clothes.RemoveRange(await resetContext.Clothes.ToListAsync());
                await resetContext.SaveChangesAsync();
            }

            var importResult = await service.ImportAsync(backupPath);

            Assert.Equal(0, importResult.RestoredImageCount);
            Assert.Equal(1, importResult.MissingImageCount);
            Assert.Contains(storedFileName, importResult.MissingImageFiles);
            Assert.True(importResult.ShouldSuggestRepair);
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

    [Fact]
    public async Task BuildDefaultBackupPath_UsesBackupsDirectoryAndZipExtension()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={Path.Combine(tempDir, "closet.db")}")
                .Options;

            var service = new BackupService(new TestDbContextFactory(options), historyDirectory: tempDir);
            var path = service.BuildDefaultBackupPath();

            Assert.EndsWith(".zip", path, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("closet-backup-", Path.GetFileName(path), StringComparison.Ordinal);
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

    [Fact]
    public async Task ClearHistoryAsync_RemovesSavedHistory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var dbPath = Path.Combine(tempDir, "closet.db");
        var historyDir = Path.Combine(tempDir, "history");

        try
        {
            var options = new DbContextOptionsBuilder<ClosetDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            await using (var context = new ClosetDbContext(options))
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
            }

            var service = new BackupService(new TestDbContextFactory(options), historyDirectory: historyDir);
            await service.ExportAsync(Path.Combine(tempDir, "empty.json"));

            Assert.NotEmpty(await service.GetHistoryAsync());

            await service.ClearHistoryAsync();

            Assert.Empty(await service.GetHistoryAsync());
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

    private static async Task CreateSourceImageAsync(string path)
    {
        using var image = new Image<Rgba32>(320, 420);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 40; y < 380; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 30; x < 290; x++)
                    row[x] = new Rgba32(230, 120, 140, 255);
            }
        });

        await image.SaveAsPngAsync(path);
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
