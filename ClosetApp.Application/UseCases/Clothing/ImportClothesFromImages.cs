using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.UseCases.Clothing;

public sealed class ImportClothesFromImages
{
    public const string DefaultName = "未命名";

    private readonly IClothingRepository _clothingRepository;
    private readonly IImageStorageService _imageStorageService;

    public ImportClothesFromImages(
        IClothingRepository clothingRepository,
        IImageStorageService imageStorageService)
    {
        _clothingRepository = clothingRepository;
        _imageStorageService = imageStorageService;
    }

    public async Task<BatchClothingImportResult> ExecuteAsync(BatchClothingImportRequest request)
    {
        if (request.Items.Count == 0)
            throw new InvalidOperationException("请选择要导入的图片。");

        var storedImagePaths = new List<string>();
        var clothes = new List<global::ClosetApp.Domain.Entities.Clothing>();

        try
        {
            foreach (var item in request.Items)
            {
                var storedImagePath = await _imageStorageService.SaveImageAsync(item.SourceImagePath);
                storedImagePaths.Add(storedImagePath);
                clothes.Add(CreateClothing(storedImagePath, item.Name, request));
            }

            await _clothingRepository.AddRangeAsync(clothes);
            return new BatchClothingImportResult(clothes);
        }
        catch
        {
            // Keep the file system aligned with the database if the import fails midway.
            await DeleteStoredImagesAsync(storedImagePaths);
            throw;
        }
    }

    private static global::ClosetApp.Domain.Entities.Clothing CreateClothing(
        string imagePath,
        string? name,
        BatchClothingImportRequest request)
    {
        return new global::ClosetApp.Domain.Entities.Clothing
        {
            Name = NormalizeName(name),
            Type = request.Type,
            Season = request.Season,
            ImagePath = imagePath,
            Color = Normalize(request.Color),
            Brand = Normalize(request.Brand),
            Notes = Normalize(request.Notes),
            FavoriteLevel = request.FavoriteLevel,
            ClothingTags = request.TagIds
                .Select(tagId => new ClothingTag { TagId = tagId })
                .ToList()
        };
    }

    private async Task DeleteStoredImagesAsync(IEnumerable<string> imagePaths)
    {
        foreach (var imagePath in imagePaths)
        {
            try
            {
                await _imageStorageService.DeleteImageWithThumbnailAsync(imagePath);
            }
            catch (Exception ex)
            {
                // Preserve the original import failure; callers can retry cleanup from settings if needed.
                _ = ex;
            }
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DefaultName : value.Trim();
    }
}
