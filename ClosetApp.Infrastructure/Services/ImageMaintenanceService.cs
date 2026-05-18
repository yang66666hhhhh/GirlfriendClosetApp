using ClosetApp.Application.DTOs;
using ClosetApp.Application.Images;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

    private async Task<List<string>> GetTrackedImagePathsAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Clothes
            .AsNoTracking()
            .Where(c => c.ImagePath != null && c.ImagePath != "")
            .Select(c => c.ImagePath!)
            .Distinct()
            .ToListAsync();
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
}
