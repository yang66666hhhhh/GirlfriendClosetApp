using ClosetApp.Application.DTOs;
using ClosetApp.Application.Images;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClosetApp.Infrastructure.Services;

public sealed class ImageMaintenanceService : IImageMaintenanceService
{
    private readonly IDbContextFactory<ClosetDbContext> _dbContextFactory;
    private readonly IImageAssetResolver _imageAssetResolver;
    private readonly IImageStorageService _imageStorageService;

    public ImageMaintenanceService(
        IDbContextFactory<ClosetDbContext> dbContextFactory,
        IImageAssetResolver imageAssetResolver,
        IImageStorageService imageStorageService)
    {
        _dbContextFactory = dbContextFactory;
        _imageAssetResolver = imageAssetResolver;
        _imageStorageService = imageStorageService;
    }

    public async Task<int> CountMissingImagesAsync()
    {
        var imagePaths = await GetTrackedImagePathsAsync();

        return imagePaths.Count(path => !_imageAssetResolver.Resolve(path).HasImage);
    }

    public async Task<int> CountMissingThumbnailsAsync()
    {
        var imagePaths = await GetTrackedImagePathsAsync();
        return imagePaths.Count(NeedsThumbnailRebuild);
    }

    public async Task<ThumbnailRebuildResult> RebuildMissingThumbnailsAsync(int maxSize = 200)
    {
        var imagePaths = await GetTrackedImagePathsAsync();
        var missingThumbnailCount = 0;
        var rebuiltCount = 0;
        var skippedCount = 0;
        var missingSourceCount = 0;

        // 统一按数据库里登记过的图片路径治理，避免同一张图被重复处理。
        foreach (var imagePath in imagePaths)
        {
            var asset = _imageAssetResolver.Resolve(imagePath);
            if (!asset.HasImage)
            {
                missingSourceCount++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(asset.DisplayPath) &&
                !string.IsNullOrWhiteSpace(asset.ThumbnailPath))
            {
                skippedCount++;
                continue;
            }

            missingThumbnailCount++;
            var displayReady = !string.IsNullOrWhiteSpace(asset.DisplayPath) ||
                               await _imageStorageService.EnsureDisplayAsync(imagePath);
            var thumbnailReady = !string.IsNullOrWhiteSpace(asset.ThumbnailPath) ||
                                 await _imageStorageService.EnsureThumbnailAsync(imagePath, maxSize);

            if (displayReady && thumbnailReady)
                rebuiltCount++;
            else
                missingSourceCount++;
        }

        return new ThumbnailRebuildResult(
            imagePaths.Count,
            missingThumbnailCount,
            rebuiltCount,
            skippedCount,
            missingSourceCount);
    }

    public async Task<int> RelinkMissingImagesAsync(string sourceDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
            throw new DirectoryNotFoundException($"目录不存在：{sourceDirectory}");

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var clothes = await context.Clothes
            .Where(c => c.ImagePath != null && c.ImagePath != "")
            .ToListAsync();

        var repairedCount = 0;
        foreach (var clothing in clothes)
        {
            var imagePath = clothing.ImagePath!;
            if (_imageAssetResolver.Resolve(imagePath).HasImage)
                continue;

            var candidate = FindMatchingFile(sourceDirectory, imagePath);
            if (candidate == null)
                continue;

            clothing.ImagePath = await _imageStorageService.SaveImageAsync(candidate);
            clothing.UpdatedAt = DateTime.Now;
            repairedCount++;
        }

        if (repairedCount > 0)
            await context.SaveChangesAsync();

        return repairedCount;
    }

    public async Task<OrphanOriginalsResult> AnalyzeOrphanOriginalsAsync()
    {
        var referencedNames = await GetReferencedImageFileNamesAsync();
        var orphanFiles = FindOrphanOriginalFiles(referencedNames);

        return new OrphanOriginalsResult(
            orphanFiles.Count,
            orphanFiles.Sum(file => new FileInfo(file).Length));
    }

    public async Task<OrphanOriginalsCleanupResult> CleanupOrphanOriginalsAsync()
    {
        var referencedNames = await GetReferencedImageFileNamesAsync();
        var orphanFiles = FindOrphanOriginalFiles(referencedNames);
        var deletedOriginalCount = 0;
        var deletedDerivedAssetCount = 0;
        long freedBytes = 0;

        foreach (var originalPath in orphanFiles)
        {
            if (!TryDeleteFile(originalPath, out var originalBytes))
                continue;

            deletedOriginalCount++;
            freedBytes += originalBytes;

            foreach (var derivedPath in GetDerivedAssetPaths(Path.GetFileName(originalPath)))
            {
                if (TryDeleteFile(derivedPath, out var derivedBytes))
                {
                    deletedDerivedAssetCount++;
                    freedBytes += derivedBytes;
                }
            }
        }

        return new OrphanOriginalsCleanupResult(
            deletedOriginalCount,
            deletedDerivedAssetCount,
            freedBytes);
    }

    private async Task<List<string>> GetTrackedImagePathsAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var clothingImagePaths = await context.Clothes
            .AsNoTracking()
            .Where(c => c.ImagePath != null && c.ImagePath != "")
            .Select(c => c.ImagePath!)
            .Distinct()
            .ToListAsync();
        var snapshotImagePaths = await GetSnapshotImagePathsAsync(context);

        return clothingImagePaths
            .Concat(snapshotImagePaths)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<List<string>> GetSnapshotImagePathsAsync(ClosetDbContext context)
    {
        var snapshots = await context.OutfitWornRecords
            .AsNoTracking()
            .Where(record => record.ClothingDetailsSnapshot != null && record.ClothingDetailsSnapshot != "")
            .Select(record => record.ClothingDetailsSnapshot!)
            .ToListAsync();

        var imagePaths = new List<string>();
        foreach (var snapshot in snapshots)
        {
            try
            {
                var clothes = JsonSerializer.Deserialize<List<ClothingSnapshotDto>>(snapshot);
                if (clothes == null)
                    continue;

                imagePaths.AddRange(clothes
                    .Select(clothing => clothing.ImagePath)
                    .Where(path => !string.IsNullOrWhiteSpace(path))!);
            }
            catch
            {
            }
        }

        return imagePaths;
    }

    private async Task<HashSet<string>> GetReferencedImageFileNamesAsync()
    {
        var imagePaths = await GetTrackedImagePathsAsync();
        return imagePaths
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;
    }

    private bool NeedsThumbnailRebuild(string imagePath)
    {
        var asset = _imageAssetResolver.Resolve(imagePath);
        return asset.HasImage &&
               (string.IsNullOrWhiteSpace(asset.DisplayPath) ||
                string.IsNullOrWhiteSpace(asset.ThumbnailPath));
    }

    private static string? FindMatchingFile(string sourceDirectory, string imagePath)
    {
        var fileName = Path.GetFileName(imagePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var directMatch = Path.Combine(sourceDirectory, fileName);
        if (File.Exists(directMatch))
            return directMatch;

        return Directory.EnumerateFiles(sourceDirectory, fileName, SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    private List<string> FindOrphanOriginalFiles(HashSet<string> referencedNames)
    {
        return _imageStorageService.GetOriginalImageFullPaths()
            .Where(path => !referencedNames.Contains(Path.GetFileName(path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IEnumerable<string> GetDerivedAssetPaths(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            yield break;

        foreach (var path in _imageStorageService.GetImageAssetFullPaths(fileName))
            yield return path;
    }

    public Task CleanupLogsAsync()
    {
        var logsDir = AppPaths.LogsDir;
        if (!Directory.Exists(logsDir))
            return Task.CompletedTask;

        var today = DateTime.Today;
        foreach (var file in Directory.EnumerateFiles(logsDir, "*.log", SearchOption.TopDirectoryOnly))
        {
            var info = new FileInfo(file);
            if (info.LastWriteTime.Date >= today)
                continue;

            try { File.Delete(file); }
            catch { /* ignore locked files */ }
        }
        return Task.CompletedTask;
    }

    public Task CleanupImageCacheAsync()
    {
        DeleteFilesInDirectory(AppPaths.DisplayDir);
        DeleteFilesInDirectory(AppPaths.ThumbnailsDir);
        return Task.CompletedTask;
    }

    public Task<int> CountFilesAsync(string directory)
    {
        if (!Directory.Exists(directory))
            return Task.FromResult(0);
        return Task.FromResult(Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Count());
    }

    public Task<long> GetDirectorySizeAsync(string directory)
    {
        if (!Directory.Exists(directory))
            return Task.FromResult(0L);
        return Task.FromResult(Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Sum(file => new FileInfo(file).Length));
    }

    private static void DeleteFilesInDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return;
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            File.Delete(file);
    }

    private static bool TryDeleteFile(string path, out long deletedBytes)
    {
        deletedBytes = 0;
        if (!File.Exists(path))
            return false;

        deletedBytes = new FileInfo(path).Length;
        File.Delete(path);
        return true;
    }
}
