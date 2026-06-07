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
    private readonly IAiAssetStorageService? _aiAssetStorageService;
    private readonly string _historyFilePath;

    public BackupService(
        IDbContextFactory<ClosetDbContext> dbContextFactory,
        IImageStorageService? imageStorageService = null,
        string? historyDirectory = null,
        IAiAssetStorageService? aiAssetStorageService = null)
    {
        _dbContextFactory = dbContextFactory;
        _imageStorageService = imageStorageService;
        var resolvedHistoryDirectory = string.IsNullOrWhiteSpace(historyDirectory)
            ? AppPaths.BackupsDir
            : historyDirectory;
        Directory.CreateDirectory(resolvedHistoryDirectory);
        _historyFilePath = Path.Combine(resolvedHistoryDirectory, BackupHistoryFileName);
        _aiAssetStorageService = aiAssetStorageService;
    }

    public async Task<BackupValidationResult> ValidateExportAsync(string filePath)
    {
        var document = await BuildBackupDocumentAsync();
        var imageAnalysis = AnalyzeImages(document);
        var format = GetBackupFormat(filePath);
        var warnings = BuildExportWarnings(filePath, format, document, imageAnalysis);

        return new BackupValidationResult(
            format,
            document.Clothes.Count,
            document.Outfits.Count,
            document.Tags.Count,
            document.WornRecords.Count,
            document.Favorites.Count,
            imageAnalysis.ReferencedCount + imageAnalysis.AiReferencedCount,
            imageAnalysis.AvailableCount + imageAnalysis.AiAvailableCount,
            imageAnalysis.MissingCount + imageAnalysis.AiMissingCount,
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
        var restoredImageCount = 0;
        var missingImageCount = 0;
        var failedRestoreImageCount = 0;
        IReadOnlyList<string> missingImageFiles = [];

        try
        {
            ClosetBackupDocument document;
            var shouldRestoreDocument = true;

            if (IsZipBackup(filePath))
            {
                (document, restoredImageCount, missingImageCount, missingImageFiles, failedRestoreImageCount) = await ImportZipAsync(filePath);
                shouldRestoreDocument = false;
            }
            else
            {
                var json = await File.ReadAllTextAsync(filePath);
                document = JsonSerializer.Deserialize<ClosetBackupDocument>(json, JsonOptions)
                    ?? throw new InvalidOperationException("备份文件格式无效。");
                missingImageFiles = GetDistinctImageReferences(document)
                    .Select(reference => reference.FileName)
                    .Concat(GetDistinctAiImageReferences(document).Select(reference => reference.FileName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                missingImageCount = missingImageFiles.Count;
            }

            if (shouldRestoreDocument)
                await RestoreDocumentAsync(document);

            var warnings = BuildImportWarnings(format, restoredImageCount, missingImageCount, document, failedRestoreImageCount);
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
                missingImageFiles,
                warnings,
                Success: true,
                FailedRestoreImageCount: failedRestoreImageCount);

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
            var failureWarnings = BuildImportFailureWarnings(format, restoredImageCount, missingImageCount);
            var failureResult = new BackupImportResult(
                filePath,
                format,
                DateTime.Now,
                0,
                0,
                0,
                0,
                0,
                restoredImageCount,
                missingImageCount,
                missingImageFiles,
                failureWarnings,
                Success: false,
                DatabaseRolledBack: true,
                CleanedUpImageCount: 0,
                FailedRestoreImageCount: failedRestoreImageCount,
                FailureStage: IsZipBackup(filePath) ? "导入并恢复图片" : "导入核心数据",
                FailureDetail: ex.Message);

            await AppendHistoryAsync(new BackupHistoryItem(
                failureResult.ImportedAt,
                "Import",
                format,
                filePath,
                0,
                Success: false,
                "导入备份失败。",
                ex.Message));
            throw new InvalidOperationException(BuildImportFailureMessage(failureResult), ex);
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
            LocalUsers = await context.LocalUsers
                .AsNoTracking()
                .OrderBy(user => user.CreatedAt)
                .ToListAsync(),
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
                    LocalUserId = c.LocalUserId,
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
                    OriginalClothingCount = o.OriginalClothingCount,
                    LocalUserId = o.LocalUserId,
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
                .ToListAsync(),
            PersonalProfile = await context.PersonalProfiles
                .AsNoTracking()
                .OrderBy(profile => profile.CreatedAt)
                .FirstOrDefaultAsync(),
            PersonalProfiles = await context.PersonalProfiles
                .AsNoTracking()
                .OrderBy(profile => profile.CreatedAt)
                .ToListAsync(),
            OutfitGeneratedImages = await context.OutfitGeneratedImages
                .AsNoTracking()
                .OrderBy(image => image.CreatedAt)
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
        var packagedBySource = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var packagedImages = new List<PackagedImage>();

        foreach (var clothing in document.Clothes)
        {
            var sourcePath = ResolveImageSourcePath(clothing.ImagePath);
            if (sourcePath == null)
                continue;

            var packagedFileName = GetOrCreatePackagedImageFileName(
                packagedBySource,
                sourcePath,
                clothing.ImagePath,
                clothing.Id,
                usedNames);
            clothing.ImagePath = packagedFileName;
            AddPackagedImage(packagedImages, sourcePath, packagedFileName);
        }

        foreach (var wornRecord in document.WornRecords)
        {
            var snapshotClothes = DeserializeSnapshotClothes(wornRecord.ClothingDetailsSnapshot);
            var changed = false;

            foreach (var snapshotClothing in snapshotClothes)
            {
                var sourcePath = ResolveImageSourcePath(snapshotClothing.ImagePath);
                if (sourcePath == null)
                    continue;

                var packagedFileName = GetOrCreatePackagedImageFileName(
                    packagedBySource,
                    sourcePath,
                    snapshotClothing.ImagePath,
                    snapshotClothing.Id,
                    usedNames);
                snapshotClothing.ImagePath = packagedFileName;
                AddPackagedImage(packagedImages, sourcePath, packagedFileName);
                changed = true;
            }

            if (changed)
                wornRecord.ClothingDetailsSnapshot = JsonSerializer.Serialize(snapshotClothes, JsonOptions);
        }

        foreach (var profile in document.PersonalProfiles)
        {
            UpdateAiImagePath(
                profile.AvatarPhotoPath,
                profile.Id,
                packagedBySource,
                usedNames,
                packagedImages,
                imagePath => profile.AvatarPhotoPath = imagePath,
                resolvePath: ResolveAiProfileImageSourcePath);

            UpdateAiImagePath(
                profile.FullBodyPhotoPath,
                profile.Id,
                packagedBySource,
                usedNames,
                packagedImages,
                imagePath => profile.FullBodyPhotoPath = imagePath,
                resolvePath: ResolveAiProfileImageSourcePath);
        }

        document.PersonalProfile = document.PersonalProfiles.OrderBy(profile => profile.CreatedAt).FirstOrDefault();

        foreach (var generatedImage in document.OutfitGeneratedImages)
        {
            UpdateAiImagePath(
                generatedImage.ResultImagePath,
                generatedImage.Id,
                packagedBySource,
                usedNames,
                packagedImages,
                imagePath => generatedImage.ResultImagePath = imagePath,
                resolvePath: ResolveAiGeneratedImageSourcePath);
        }

        return packagedImages;
    }

    private async Task<(ClosetBackupDocument Document, int RestoredImageCount, int MissingImageCount, IReadOnlyList<string> MissingImageFiles, int FailedRestoreImageCount)> ImportZipAsync(string filePath)
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
        var extractedImageCount = 0;
        var failedRestoreImageCount = 0;
        var missingImageFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var extractedImages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var imageReference in GetImageReferences(document))
            {
                var imageFileName = imageReference.FileName;
                if (string.IsNullOrWhiteSpace(imageFileName))
                    continue;

                if (extractedImages.ContainsKey(imageFileName))
                {
                    imageReference.ApplyRestoredPath(imageFileName);
                    continue;
                }

                var imageEntry = FindZipEntry(archive, $"{ImagesEntryFolder}{imageFileName}");
                if (imageEntry == null)
                {
                    missingImageFiles.Add(imageFileName);
                    imageReference.ApplyRestoredPath(imageFileName);
                    continue;
                }

                var tempImagePath = Path.Combine(tempDir, imageFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(tempImagePath)!);
                imageEntry.ExtractToFile(tempImagePath, overwrite: true);
                imageReference.ApplyRestoredPath(imageFileName);
                extractedImages[imageFileName] = "wardrobe";
                extractedImageCount++;
            }

            foreach (var imageReference in GetAiImageReferences(document))
            {
                var imageFileName = imageReference.FileName;
                if (string.IsNullOrWhiteSpace(imageFileName))
                    continue;

                if (extractedImages.ContainsKey(imageFileName))
                {
                    imageReference.ApplyRestoredPath(imageFileName);
                    continue;
                }

                var imageEntry = FindZipEntry(archive, $"{ImagesEntryFolder}{imageFileName}");
                if (imageEntry == null)
                {
                    missingImageFiles.Add(imageFileName);
                    imageReference.ApplyRestoredPath(imageFileName);
                    continue;
                }

                var tempImagePath = Path.Combine(tempDir, imageFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(tempImagePath)!);
                imageEntry.ExtractToFile(tempImagePath, overwrite: true);
                imageReference.ApplyRestoredPath(imageFileName);
                extractedImages[imageFileName] = imageReference.Kind;
                extractedImageCount++;
            }

            await RestoreDocumentAsync(document);

            foreach (var restoredImage in extractedImages)
            {
                try
                {
                    var tempImagePath = Path.Combine(tempDir, restoredImage.Key);
                    if (string.Equals(restoredImage.Value, "profile", StringComparison.OrdinalIgnoreCase))
                    {
                        await _aiAssetStorageService!.RestoreProfileReferenceImageAsync(tempImagePath, restoredImage.Key);
                    }
                    else if (string.Equals(restoredImage.Value, "render", StringComparison.OrdinalIgnoreCase))
                    {
                        await _aiAssetStorageService!.RestoreGeneratedImageAsync(tempImagePath, restoredImage.Key);
                    }
                    else
                    {
                        await _imageStorageService!.RestoreImageAsync(tempImagePath, restoredImage.Key);
                    }
                }
                catch
                {
                    failedRestoreImageCount++;
                }
            }

            var orderedMissingFiles = missingImageFiles
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return (document, extractedImageCount - failedRestoreImageCount, orderedMissingFiles.Count, orderedMissingFiles, failedRestoreImageCount);
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

        var defaultUserId = EnsureBackupUsers(document);
        context.LocalUsers.AddRange(document.LocalUsers);

        foreach (var tag in document.Tags)
            tag.LocalUserId ??= defaultUserId;
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
                LocalUserId = item.LocalUserId ?? defaultUserId
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
                WearCount = item.WearCount,
                OriginalClothingCount = item.OriginalClothingCount,
                LocalUserId = item.LocalUserId ?? defaultUserId
            };

            outfit.OutfitClothes = item.ClothingIds
                .Select(clothingId => new OutfitClothing { OutfitId = outfit.Id, ClothingId = clothingId })
                .ToList();
            return outfit;
        }).ToList();
        context.Outfits.AddRange(outfits);

        foreach (var record in document.WornRecords)
            record.LocalUserId ??= defaultUserId;
        context.OutfitWornRecords.AddRange(document.WornRecords);

        foreach (var favorite in document.Favorites)
            favorite.LocalUserId ??= defaultUserId;
        context.Favorites.AddRange(document.Favorites);

        if (document.PersonalProfiles.Count == 0 && document.PersonalProfile != null)
            document.PersonalProfiles.Add(document.PersonalProfile);

        foreach (var profile in document.PersonalProfiles)
            profile.LocalUserId ??= defaultUserId;
        context.PersonalProfiles.AddRange(document.PersonalProfiles);

        foreach (var image in document.OutfitGeneratedImages)
            image.LocalUserId ??= defaultUserId;
        context.OutfitGeneratedImages.AddRange(document.OutfitGeneratedImages);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static Guid EnsureBackupUsers(ClosetBackupDocument document)
    {
        if (document.LocalUsers.Count == 0)
        {
            document.LocalUsers.Add(new LocalUser
            {
                Id = Guid.NewGuid(),
                DisplayName = "私人衣橱",
                Role = Domain.Enums.LocalUserRole.SuperAdmin,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        var superAdmin = document.LocalUsers
            .Where(user => user.Role == Domain.Enums.LocalUserRole.SuperAdmin)
            .OrderBy(user => user.CreatedAt)
            .FirstOrDefault();

        if (superAdmin == null)
        {
            superAdmin = document.LocalUsers.OrderBy(user => user.CreatedAt).First();
            superAdmin.Role = Domain.Enums.LocalUserRole.SuperAdmin;
            superAdmin.IsActive = true;
        }

        return superAdmin.Id;
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

    private static string GetOrCreatePackagedImageFileName(
        IDictionary<string, string> packagedBySource,
        string sourcePath,
        string? currentImagePath,
        Guid ownerId,
        ISet<string> usedNames)
    {
        var normalizedSourcePath = Path.GetFullPath(sourcePath);
        if (packagedBySource.TryGetValue(normalizedSourcePath, out var existingName))
            return existingName;

        var packagedFileName = BuildPackagedImageFileName(currentImagePath, ownerId, sourcePath, usedNames);
        packagedBySource[normalizedSourcePath] = packagedFileName;
        return packagedFileName;
    }

    private static string BuildPackagedImageFileName(
        string? imagePath,
        Guid ownerId,
        string sourcePath,
        ISet<string> usedNames)
    {
        var extension = Path.GetExtension(sourcePath);
        var currentName = string.IsNullOrWhiteSpace(imagePath)
            ? null
            : Path.GetFileName(imagePath);

        var candidate = string.IsNullOrWhiteSpace(currentName)
            ? $"{ownerId}{extension}"
            : currentName;

        if (usedNames.Add(candidate))
            return candidate;

        var baseName = Path.GetFileNameWithoutExtension(candidate);
        var suffix = 1;
        while (!usedNames.Add($"{baseName}_{suffix}{extension}"))
            suffix++;

        return $"{baseName}_{suffix}{extension}";
    }

    private static void AddPackagedImage(
        ICollection<PackagedImage> packagedImages,
        string sourcePath,
        string packagedFileName)
    {
        if (packagedImages.Any(image =>
                string.Equals(image.EntryFileName, packagedFileName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        packagedImages.Add(new PackagedImage(sourcePath, packagedFileName));
    }

    private void UpdateAiImagePath(
        string? imagePath,
        Guid ownerId,
        IDictionary<string, string> packagedBySource,
        ISet<string> usedNames,
        ICollection<PackagedImage> packagedImages,
        Action<string> applyPackagedPath,
        Func<string?, string?> resolvePath)
    {
        var sourcePath = resolvePath(imagePath);
        if (sourcePath == null)
            return;

        var packagedFileName = GetOrCreatePackagedImageFileName(
            packagedBySource,
            sourcePath,
            imagePath,
            ownerId,
            usedNames);

        applyPackagedPath(packagedFileName);
        AddPackagedImage(packagedImages, sourcePath, packagedFileName);
    }

    private static ZipArchiveEntry? FindZipEntry(ZipArchive archive, string entryName)
    {
        return archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.FullName, entryName, StringComparison.OrdinalIgnoreCase));
    }

    private BackupImageAnalysis AnalyzeImages(ClosetBackupDocument document)
    {
        var referencedCount = 0;
        var availableCount = 0;

        foreach (var imageReference in GetDistinctImageReferences(document))
        {
            referencedCount++;
            if (ResolveImageSourcePath(imageReference.ImagePath) != null)
                availableCount++;
        }

        var aiReferencedCount = 0;
        var aiAvailableCount = 0;

        foreach (var imageReference in GetDistinctAiImageReferences(document))
        {
            aiReferencedCount++;
            if (ResolveAiImageSourcePath(imageReference.ImagePath, imageReference.Kind) != null)
                aiAvailableCount++;
        }

        return new BackupImageAnalysis(
            referencedCount,
            availableCount,
            referencedCount - availableCount,
            aiReferencedCount,
            aiAvailableCount,
            aiReferencedCount - aiAvailableCount);
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

        if (format == "json" && (imageAnalysis.ReferencedCount > 0 || imageAnalysis.AiReferencedCount > 0))
            warnings.Add("JSON 备份只保存核心数据，不会打包图片文件。");

        var missingCount = imageAnalysis.MissingCount + imageAnalysis.AiMissingCount;
        if (format == "zip" && missingCount > 0)
            warnings.Add($"有 {missingCount} 张图片路径已失效，ZIP 备份不会包含这些文件。");

        return warnings;
    }

    private static List<string> BuildImportWarnings(
        string format,
        int restoredImageCount,
        int missingImageCount,
        ClosetBackupDocument document,
        int failedRestoreImageCount = 0)
    {
        var warnings = new List<string>();

        if (format == "json" && (HasImageReferences(document) || HasAiImageReferences(document)))
            warnings.Add("JSON 备份不会附带图片文件，导入后如出现缺图，可使用“图片修复”。");

        if (format == "zip" && missingImageCount > 0)
            warnings.Add($"备份包里有 {missingImageCount} 张图片缺失，相关衣物会保留原图片文件名。");

        if (format == "zip" && failedRestoreImageCount > 0)
            warnings.Add($"有 {failedRestoreImageCount} 张图片在恢复到本地时失败，核心数据已导入，可继续用“图片修复”补齐。");

        if (format == "zip" && restoredImageCount == 0 && (HasImageReferences(document) || HasAiImageReferences(document)))
            warnings.Add("备份里有图片路径，但没有恢复到任何图片文件。");

        return warnings;
    }

    private static List<string> BuildImportFailureWarnings(string format, int restoredImageCount, int missingImageCount)
    {
        var warnings = new List<string>
        {
            "本次导入没有完成，数据库改动已回滚。"
        };

        if (format == "zip" && restoredImageCount > 0)
            warnings.Add($"已尝试恢复 {restoredImageCount} 张图片，请确认图片目录中是否需要手动复查。");

        if (format == "zip" && missingImageCount > 0)
            warnings.Add($"备份包中仍有 {missingImageCount} 张图片缺失。");

        return warnings;
    }

    private static string BuildImportFailureMessage(BackupImportResult result)
    {
        var parts = new List<string>
        {
            "导入备份失败。",
            "当前数据库已保持导入前状态。"
        };

        if (result.RestoredImageCount > 0)
            parts.Add($"导入过程中曾恢复 {result.RestoredImageCount} 张图片。");

        if (result.MissingImageCount > 0)
            parts.Add($"备份包中还有 {result.MissingImageCount} 张图片缺失。");

        if (!string.IsNullOrWhiteSpace(result.FailureDetail))
            parts.Add(result.FailureDetail!);

        return string.Join(" ", parts);
    }

    private static bool HasImageReferences(ClosetBackupDocument document)
    {
        return GetImageReferences(document).Any();
    }

    private static bool HasAiImageReferences(ClosetBackupDocument document)
    {
        return GetAiImageReferences(document).Any();
    }

    private static IEnumerable<ImageReference> GetDistinctImageReferences(ClosetBackupDocument document)
    {
        return GetImageReferences(document)
            .GroupBy(reference => reference.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());
    }

    private static IEnumerable<AiImageReference> GetDistinctAiImageReferences(ClosetBackupDocument document)
    {
        return GetAiImageReferences(document)
            .GroupBy(reference => $"{reference.Kind}:{reference.FileName}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());
    }

    private static List<ImageReference> GetImageReferences(ClosetBackupDocument document)
    {
        var references = new List<ImageReference>();

        foreach (var clothing in document.Clothes)
            AddReference(references, clothing.ImagePath, fileName => clothing.ImagePath = fileName);

        foreach (var wornRecord in document.WornRecords)
        {
            var snapshotClothes = DeserializeSnapshotClothes(wornRecord.ClothingDetailsSnapshot);
            if (snapshotClothes.Count == 0)
                continue;

            foreach (var snapshotClothing in snapshotClothes)
            {
                AddReference(references, snapshotClothing.ImagePath, fileName =>
                {
                    snapshotClothing.ImagePath = fileName;
                    wornRecord.ClothingDetailsSnapshot = JsonSerializer.Serialize(snapshotClothes, JsonOptions);
                });
            }
        }

        return references;
    }

    private static List<AiImageReference> GetAiImageReferences(ClosetBackupDocument document)
    {
        var references = new List<AiImageReference>();

        if (document.PersonalProfiles.Count == 0 && document.PersonalProfile != null)
            document.PersonalProfiles.Add(document.PersonalProfile);

        foreach (var profile in document.PersonalProfiles)
        {
            AddAiReference(references, profile.AvatarPhotoPath, "profile", fileName => profile.AvatarPhotoPath = fileName);
            AddAiReference(references, profile.FullBodyPhotoPath, "profile", fileName => profile.FullBodyPhotoPath = fileName);
        }

        foreach (var image in document.OutfitGeneratedImages)
            AddAiReference(references, image.ResultImagePath, "render", fileName => image.ResultImagePath = fileName);

        return references;
    }

    private static void AddReference(
        ICollection<ImageReference> references,
        string? imagePath,
        Action<string> applyRestoredPath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        var fileName = Path.GetFileName(imagePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        references.Add(new ImageReference(imagePath, fileName, applyRestoredPath));
    }

    private static void AddAiReference(
        ICollection<AiImageReference> references,
        string? imagePath,
        string kind,
        Action<string> applyRestoredPath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        var fileName = Path.GetFileName(imagePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        references.Add(new AiImageReference(imagePath, fileName, kind, applyRestoredPath));
    }

    private static List<ClothingSnapshotDto> DeserializeSnapshotClothes(string? snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ClothingSnapshotDto>>(snapshotJson, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
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

    private string? ResolveAiProfileImageSourcePath(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return null;

        EnsureAiAssetStorageAvailable();

        var path = _aiAssetStorageService!.GetProfileReferenceFullPath(imagePath);
        return File.Exists(path) ? path : null;
    }

    private string? ResolveAiGeneratedImageSourcePath(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return null;

        EnsureAiAssetStorageAvailable();

        var path = _aiAssetStorageService!.GetGeneratedImageFullPath(imagePath);
        return File.Exists(path) ? path : null;
    }

    private string? ResolveAiImageSourcePath(string? imagePath, string kind)
    {
        return string.Equals(kind, "profile", StringComparison.OrdinalIgnoreCase)
            ? ResolveAiProfileImageSourcePath(imagePath)
            : ResolveAiGeneratedImageSourcePath(imagePath);
    }

    private void EnsureAiAssetStorageAvailable()
    {
        if (_aiAssetStorageService == null)
            throw new InvalidOperationException("当前备份服务未配置 AI 资产存储服务，无法处理 AI 参考图和生成图。");
    }

    private sealed record ImageReference(string ImagePath, string FileName, Action<string> ApplyRestoredPath);

    private sealed record AiImageReference(string ImagePath, string FileName, string Kind, Action<string> ApplyRestoredPath);

    private sealed record BackupImageAnalysis(
        int ReferencedCount,
        int AvailableCount,
        int MissingCount,
        int AiReferencedCount,
        int AiAvailableCount,
        int AiMissingCount);

    private static async Task ClearExistingDataAsync(ClosetDbContext context)
    {
        context.OutfitWornRecords.RemoveRange(await context.OutfitWornRecords.ToListAsync());
        context.Favorites.RemoveRange(await context.Favorites.ToListAsync());
        context.OutfitGeneratedImages.RemoveRange(await context.OutfitGeneratedImages.ToListAsync());
        context.PersonalProfiles.RemoveRange(await context.PersonalProfiles.ToListAsync());
        context.OutfitClothes.RemoveRange(await context.OutfitClothes.ToListAsync());
        context.ClothingTags.RemoveRange(await context.ClothingTags.ToListAsync());
        context.Outfits.RemoveRange(await context.Outfits.ToListAsync());
        context.Clothes.RemoveRange(await context.Clothes.ToListAsync());
        context.Tags.RemoveRange(await context.Tags.ToListAsync());
        context.LocalUsers.RemoveRange(await context.LocalUsers.ToListAsync());
        await context.SaveChangesAsync();
    }
}
