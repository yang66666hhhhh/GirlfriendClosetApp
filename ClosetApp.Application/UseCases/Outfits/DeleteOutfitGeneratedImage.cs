using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.UseCases.Outfits;

public sealed class DeleteOutfitGeneratedImage
{
    private readonly IOutfitGeneratedImageRepository _repository;
    private readonly IAiAssetStorageService _assetStorageService;

    public DeleteOutfitGeneratedImage(
        IOutfitGeneratedImageRepository repository,
        IAiAssetStorageService assetStorageService)
    {
        _repository = repository;
        _assetStorageService = assetStorageService;
    }

    public async Task ExecuteAsync(Guid imageId)
    {
        var image = await _repository.GetByIdAsync(imageId);
        if (image == null)
            return;

        var outfitId = image.OutfitId;
        var wasPrimary = image.IsPrimary;
        await _repository.DeleteAsync(imageId);
        await _assetStorageService.TryDeleteGeneratedImageAsync(image.ResultImagePath);

        if (!wasPrimary)
            return;

        var remainingImages = await _repository.GetByOutfitIdAsync(outfitId);
        var promotedImage = remainingImages
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();
        if (promotedImage == null)
            return;

        promotedImage.IsPrimary = true;
        promotedImage.UpdatedAt = DateTime.Now;
        await _repository.UpdateAsync(promotedImage);
    }
}
