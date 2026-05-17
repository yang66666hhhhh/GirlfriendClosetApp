using System.IO.Compression;
using System.Text.Json;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClosetApp.Infrastructure.Services;

public sealed class BackupService : IBackupService
{
    private const string BackupDocumentEntryName = "backup.json";
    private const string BackupHistoryFileName = "backup-history.json";
    private const string ImagesEntryFolder = "images/";
    private const int MaxHistoryEntries = 24;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IDbContextFactory<ClosetDbContext> _dbContextFactory;
    private readonly IImageStorageService? _imageStorageService;
    private readonly string _historyFilePath;

    public BackupService(
        IDbContextFactory<ClosetDbContext> dbContextFactory,
        IImageStorageService? imageStorageService = null,
        string? historyDirectory = null)
    {
        _dbContextFactory = dbContextFactory;
        _imageStorageService = imageStorageService;
        var resolvedHistoryDirectory = string.IsNullOrWhiteSpace(historyDirectory)
            ? AppPaths.BackupsDir
            : historyDirectory;
        Directory.CreateDirectory(resolvedHistoryDirectory);
        _historyFilePath = Path.Combine(resolvedHistoryDirectory, BackupHistoryFileName);
    }

    public async Task<BackupValidationResult> ValidateExportAsync(string filePath)
    {
        var document = await BuildBackupDocumentAsync();
        var imageAnalysis = AnalyzeImages(document.Clothes);
        var format = GetBackupFormat(filePath);
        var warnings = BuildExportWarnings(filePath, format, document, imageAnalysis);

        return new BackupValidationResult(
            format,
            document.Clothes.Count,
            document.Outfits.Count,
            document.Tags.Count,
            document.WornRecords.Count,
            document.Favorites.Count,
            imageAnalysis.ReferencedCount,
            imageAnalysis.AvailableCount,
            imageAnalysis.MissingCount,
            warnings);
    }

    public async Task<BackupExportResult> ExportAsync(string filePath)
    {
        var validation = await ValidateExportAsync(filePath);
        var document = await BuildBackupDocumentAsync();

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        try
        {
            var includedImageCount = 0;
            if (IsZipBackup(filePath))
            {
                EnsureImageStorageAvailable();
                includedImageCount = await ExportZipAsync(filePath, document);
            }
            else
            {
                await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(document, JsonOptions));
            }

            var fileInfo = new FileInfo(filePath);
            var result = new BackupExportResult(
                filePath,
                validation.Format,
                DateTime.Now,
                fileInfo.Exists ? fileInfo.Length : 0,
                validation.ClothingCount,
                validation.OutfitCount,
                validation.TagCount,
                validation.WornRecordCount,
                validation.FavoriteCount,
                includedImageCount,
                validation.MissingImageCount,
                validation.Warnings);

            await AppendHistoryAsync(new BackupHistoryItem(
                result.ExportedAt,
                "Export",
                result.Format,
                result.FilePath,
                result.FileSizeBytes,
                Success: true,
                result.Summary));

            return result;
        }
        catch (Exception ex)
        {
            await AppendHistoryAsync(new BackupHistoryItem(
                DateTime.Now,
                "Export",
                validation.Format,
                filePath,
                0,
                Success: false,
                "导出备份失败。",
                ex.Message));
            throw;
        }
    }

    public async Task<BackupImportResult> ImportAsync(string filePath)
    {
        var format = GetBackupFormat(filePath);

        try
        {
            ClosetBackupDocument document;
            var restoredImageCount = 0;
            var missingImageCount = 0;

            if (IsZipBackup(filePath))
            {
                EnsureImageStorageAvailable();
                (document, restoredImageCount, missingImageCount) = await ImportZipAsync(filePath);
            }
            else
            {
                var json = await File.ReadAllTextAsync(filePath);
                document = JsonSerializer.Deserialize<ClosetBackupDocument>(json, JsonOptions)
                    ?? throw new InvalidOperationException("备份文件格式无效。");
                missingImageCount = document.Clothes.Count(c => !string.IsNullOrWhiteSpace(c.ImagePath));
            }

            await RestoreDocumentAsync(document);

            var warnings = BuildImportWarnings(format, restoredImageCount, missingImageCount, document);
            var result = new BackupImportResult(
                filePath,
                format,
                DateTime.Now,
                document.Clothes.Count,
                document.Outfits.Count,
                document.Tags.Count,
                document.WornRecords.Count,
                document.Favorites.Count,
                restoredImageCount,
                missingImageCount,
                warnings);

            var fileInfo = new FileInfo(filePath);
            await AppendHistoryAsync(new BackupHistoryItem(
                result.ImportedAt,
                "Import",
                result.Format,
                result.FilePath,
                fileInfo.Exists ? fileInfo.Length : 0,
                Success: true,
                result.Summary));

            return result;
        }
        catch (Exception ex)
        {
            await AppendHistoryAsync(new BackupHistoryItem(
                DateTime.Now,
                "Import",
                format,
                filePath,
                0,
                Success: false,
                "导入备份失败。",
                ex.Message));
            throw;
        }
    }

    public async Task<IReadOnlyList<BackupHistoryItem>> GetHistoryAsync(int maxCount = 8)
    {
        var history = await LoadHistoryAsync();
        return history
            .OrderByDescending(item => item.Timestamp)
            .Take(maxCount)
            .ToList();
    }

    public Task ClearHistoryAsync()
    {
        if (File.Exists(_historyFilePath))
            File.Delete(_historyFilePath);

        return Task.CompletedTask;
    }

    public string BuildDefaultBackupPath()
    {
        return Path.Combine(AppPaths.BackupsDir, $"closet-backup-{DateTime.Now:yyyyMMdd-HHmm}.zip");
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

    private async Task<int> ExportZipAsync(string filePath, ClosetBackupDocument document)
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

        return packagedImages.Count;
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

    private async Task<(ClosetBackupDocument Document, int RestoredImageCount, int MissingImageCount)> ImportZipAsync(string filePath)
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
        var restoredImageCount = 0;
        var missingImageCount = 0;

        try
        {
            foreach (var clothing in document.Clothes.Where(c => !string.IsNullOrWhiteSpace(c.ImagePath)))
            {
                var imageFileName = Path.GetFileName(clothing.ImagePath);
                if (string.IsNullOrWhiteSpace(imageFileName))
                    continue;

                var imageEntry = FindZipEntry(archive, $"{ImagesEntryFolder}{imageFileName}");
                if (imageEntry == null)
                {
                    missingImageCount++;
                    continue;
                }

                var tempImagePath = Path.Combine(tempDir, imageFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(tempImagePath)!);
                imageEntry.ExtractToFile(tempImagePath, overwrite: true);
                await _imageStorageService!.RestoreImageAsync(tempImagePath, imageFileName);
                clothing.ImagePath = imageFileName;
                restoredImageCount++;
            }

            return (document, restoredImageCount, missingImageCount);
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

    private static string GetBackupFormat(string filePath)
    {
        return IsZipBackup(filePath) ? "zip" : "json";
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

    private BackupImageAnalysis AnalyzeImages(IEnumerable<ClothingBackupItem> clothes)
    {
        var referencedCount = 0;
        var availableCount = 0;

        foreach (var clothing in clothes)
        {
            if (string.IsNullOrWhiteSpace(clothing.ImagePath))
                continue;

            referencedCount++;
            if (ResolveImageSourcePath(clothing.ImagePath) != null)
                availableCount++;
        }

        return new BackupImageAnalysis(referencedCount, availableCount, referencedCount - availableCount);
    }

    private List<string> BuildExportWarnings(
        string filePath,
        string format,
        ClosetBackupDocument document,
        BackupImageAnalysis imageAnalysis)
    {
        var warnings = new List<string>();

        if (document.Clothes.Count == 0 &&
            document.Outfits.Count == 0 &&
            document.Tags.Count == 0 &&
            document.WornRecords.Count == 0 &&
            document.Favorites.Count == 0)
        {
            warnings.Add("当前没有可导出的数据，这会生成一个空备份。");
        }

        if (File.Exists(filePath))
            warnings.Add("目标文件已存在，导出后会覆盖旧文件。");

        if (format == "json" && imageAnalysis.ReferencedCount > 0)
            warnings.Add("JSON 备份只保存核心数据，不会打包图片文件。");

        if (format == "zip" && imageAnalysis.MissingCount > 0)
            warnings.Add($"有 {imageAnalysis.MissingCount} 张图片路径已失效，ZIP 备份不会包含这些文件。");

        return warnings;
    }

    private static List<string> BuildImportWarnings(
        string format,
        int restoredImageCount,
        int missingImageCount,
        ClosetBackupDocument document)
    {
        var warnings = new List<string>();

        if (format == "json" && document.Clothes.Any(c => !string.IsNullOrWhiteSpace(c.ImagePath)))
            warnings.Add("JSON 备份不会附带图片文件，导入后如出现缺图，可使用“图片修复”。");

        if (format == "zip" && missingImageCount > 0)
            warnings.Add($"备份包里有 {missingImageCount} 张图片缺失，相关衣物会保留原图片文件名。");

        if (format == "zip" && restoredImageCount == 0 && document.Clothes.Any(c => !string.IsNullOrWhiteSpace(c.ImagePath)))
            warnings.Add("备份里有图片路径，但没有恢复到任何图片文件。");

        return warnings;
    }

    private async Task<List<BackupHistoryItem>> LoadHistoryAsync()
    {
        if (!File.Exists(_historyFilePath))
            return [];

        await using var stream = File.OpenRead(_historyFilePath);
        return await JsonSerializer.DeserializeAsync<List<BackupHistoryItem>>(stream, JsonOptions) ?? [];
    }

    private async Task AppendHistoryAsync(BackupHistoryItem item)
    {
        var history = await LoadHistoryAsync();
        history.Insert(0, item);
        if (history.Count > MaxHistoryEntries)
            history = history.Take(MaxHistoryEntries).ToList();

        await using var stream = File.Create(_historyFilePath);
        await JsonSerializer.SerializeAsync(stream, history, JsonOptions);
    }

    private sealed record PackagedImage(string SourcePath, string EntryFileName);

    private sealed record BackupImageAnalysis(int ReferencedCount, int AvailableCount, int MissingCount);

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
