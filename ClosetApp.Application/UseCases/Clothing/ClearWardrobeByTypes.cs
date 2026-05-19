using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.UseCases.Clothing;

public sealed class ClearWardrobeByTypes
{
    private readonly IClothingRepository _clothingRepository;
    private readonly IOutfitRepository _outfitRepository;
    private readonly IImageStorageService _imageStorageService;

    public ClearWardrobeByTypes(
        IClothingRepository clothingRepository,
        IOutfitRepository outfitRepository,
        IImageStorageService imageStorageService)
    {
        _clothingRepository = clothingRepository;
        _outfitRepository = outfitRepository;
        _imageStorageService = imageStorageService;
    }

    public async Task<BatchWardrobeClearResult> ExecuteAsync(BatchWardrobeClearRequest request)
    {
        var selectedTypes = request.Types.Distinct().ToList();
        if (selectedTypes.Count == 0)
            throw new InvalidOperationException("请至少选择一个要清空的分类。");

        var clothes = (await _clothingRepository.GetByTypesAsync(selectedTypes)).ToList();
        if (clothes.Count == 0)
            return new BatchWardrobeClearResult(0);

        // Commit the database deletion first, then clean image assets best-effort.
        await _clothingRepository.DeleteRangeAsync(clothes.Select(clothing => clothing.Id));
        await _outfitRepository.DeleteEmptyOutfitsAsync();

        foreach (var imagePath in clothes
            .Select(clothing => clothing.ImagePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await _imageStorageService.DeleteImageWithThumbnailAsync(imagePath!);
            }
            catch
            {
            }
        }

        return new BatchWardrobeClearResult(clothes.Count);
    }
}
