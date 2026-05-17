using System.IO.Compression;
using System.Text.Json;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClosetApp.Infrastructure.Services;

public sealed class BackupService : IBackupService
{
    private const string BackupDocumentEntryName = "backup.json";
    private const string ImagesEntryFolder = "images/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IDbContextFactory<ClosetDbContext> _dbContextFactory;
    private readonly IImageStorageService? _imageStorageService;

    public BackupService(IDbContextFactory<ClosetDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public BackupService(
        IDbContextFactory<ClosetDbContext> dbContextFactory,
        IImageStorageService imageStorageService) : this(dbContextFactory)
    {
        _imageStorageService = imageStorageService;
    }

    public async Task ExportAsync(string filePath)
    {
        var document = await BuildBackupDocumentAsync();

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        if (IsZipBackup(filePath))
        {
            EnsureImageStorageAvailable();
            await ExportZipAsync(filePath, document);
            return;
        }

        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(document, JsonOptions));
    }

    public async Task ImportAsync(string filePath)
    {
        if (IsZipBackup(filePath))
        {
            EnsureImageStorageAvailable();
            await ImportZipAsync(filePath);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var document = JsonSerializer.Deserialize<ClosetBackupDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("备份文件格式无效。");
        await RestoreDocumentAsync(document);
    }

    private async Task<ClosetBackupDocument> BuildBackupDocumentAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return new ClosetBackupDocument
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
    }

    private async Task ExportZipAsync(string filePath, ClosetBackupDocument document)
    {
        var packagedImages = PreparePackagedImages(document);

        if (File.Exists(filePath))
            File.Delete(filePath);

        using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);

        var documentEntry = archive.CreateEntry(BackupDocumentEntryName, CompressionLevel.Optimal);
        await using (var jsonStream = documentEntry.Open())
        {
            await JsonSerializer.SerializeAsync(jsonStream, document, JsonOptions);
        }

        foreach (var image in packagedImages)
        {
            var imageEntry = archive.CreateEntry($"{ImagesEntryFolder}{image.EntryFileName}", CompressionLevel.Optimal);
            await using var entryStream = imageEntry.Open();
            await using var sourceStream = File.OpenRead(image.SourcePath);
            await sourceStream.CopyToAsync(entryStream);
        }
    }

    private List<PackagedImage> PreparePackagedImages(ClosetBackupDocument document)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var packagedImages = new List<PackagedImage>();

        foreach (var clothing in document.Clothes)
        {
            var sourcePath = ResolveImageSourcePath(clothing.ImagePath);
            if (sourcePath == null)
                continue;

            var packagedFileName = BuildPackagedImageFileName(clothing, sourcePath, usedNames);
            clothing.ImagePath = packagedFileName;
            packagedImages.Add(new PackagedImage(sourcePath, packagedFileName));
        }

        return packagedImages;
    }

    private async Task ImportZipAsync(string filePath)
    {
        using var archive = ZipFile.OpenRead(filePath);
        var documentEntry = archive.GetEntry(BackupDocumentEntryName)
            ?? throw new InvalidOperationException("备份包缺少 backup.json。");

        ClosetBackupDocument document;
        await using (var jsonStream = documentEntry.Open())
        {
            document = (await JsonSerializer.DeserializeAsync<ClosetBackupDocument>(jsonStream, JsonOptions))
                ?? throw new InvalidOperationException("备份文件格式无效。");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Import", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            foreach (var clothing in document.Clothes.Where(c => !string.IsNullOrWhiteSpace(c.ImagePath)))
            {
                var imageFileName = Path.GetFileName(clothing.ImagePath);
                if (string.IsNullOrWhiteSpace(imageFileName))
                    continue;

                var imageEntry = FindZipEntry(archive, $"{ImagesEntryFolder}{imageFileName}");
                if (imageEntry == null)
                    continue;

                var tempImagePath = Path.Combine(tempDir, imageFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(tempImagePath)!);
                imageEntry.ExtractToFile(tempImagePath, overwrite: true);
                await _imageStorageService!.RestoreImageAsync(tempImagePath, imageFileName);
                clothing.ImagePath = imageFileName;
            }

            await RestoreDocumentAsync(document);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    private async Task RestoreDocumentAsync(ClosetBackupDocument document)
    {

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

    private static bool IsZipBackup(string filePath)
    {
        return string.Equals(Path.GetExtension(filePath), ".zip", StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureImageStorageAvailable()
    {
        if (_imageStorageService == null)
            throw new InvalidOperationException("当前备份服务未配置图片存储服务，无法处理 ZIP 备份包。");
    }

    private string? ResolveImageSourcePath(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return null;

        if (Path.IsPathRooted(imagePath) && File.Exists(imagePath))
            return imagePath;

        if (File.Exists(imagePath))
            return Path.GetFullPath(imagePath);

        if (_imageStorageService == null)
            return null;

        var storedImagePath = _imageStorageService.GetImageFullPath(imagePath);
        return File.Exists(storedImagePath) ? storedImagePath : null;
    }

    private static string BuildPackagedImageFileName(
        ClothingBackupItem clothing,
        string sourcePath,
        ISet<string> usedNames)
    {
        var extension = Path.GetExtension(sourcePath);
        var currentName = string.IsNullOrWhiteSpace(clothing.ImagePath)
            ? null
            : Path.GetFileName(clothing.ImagePath);

        var candidate = string.IsNullOrWhiteSpace(currentName)
            ? $"{clothing.Id}{extension}"
            : currentName;

        if (usedNames.Add(candidate))
            return candidate;

        var baseName = Path.GetFileNameWithoutExtension(candidate);
        var suffix = 1;
        while (!usedNames.Add($"{baseName}_{suffix}{extension}"))
            suffix++;

        return $"{baseName}_{suffix}{extension}";
    }

    private static ZipArchiveEntry? FindZipEntry(ZipArchive archive, string entryName)
    {
        return archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.FullName, entryName, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record PackagedImage(string SourcePath, string EntryFileName);

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
