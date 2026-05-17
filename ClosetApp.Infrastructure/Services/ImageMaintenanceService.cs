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
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var imagePaths = await context.Clothes
            .AsNoTracking()
            .Where(c => c.ImagePath != null && c.ImagePath != "")
            .Select(c => c.ImagePath!)
            .ToListAsync();

        return imagePaths.Count(path => !_imageAssetResolver.Resolve(path).HasImage);
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
