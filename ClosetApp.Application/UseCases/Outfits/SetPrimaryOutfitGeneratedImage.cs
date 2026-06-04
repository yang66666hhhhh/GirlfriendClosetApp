using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.UseCases.Outfits;

public sealed class SetPrimaryOutfitGeneratedImage
{
    private readonly IOutfitGeneratedImageRepository _repository;

    public SetPrimaryOutfitGeneratedImage(IOutfitGeneratedImageRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(Guid imageId)
    {
        var image = await _repository.GetByIdAsync(imageId)
            ?? throw new InvalidOperationException("生成图片不存在。");

        await _repository.ClearPrimaryAsync(image.OutfitId, image.Id);
        image.IsPrimary = true;
        image.UpdatedAt = DateTime.Now;
        await _repository.UpdateAsync(image);
    }
}
