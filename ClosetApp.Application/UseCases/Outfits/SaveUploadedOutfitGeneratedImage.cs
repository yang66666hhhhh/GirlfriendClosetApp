using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.UseCases.Outfits;

public sealed class SaveUploadedOutfitGeneratedImage
{
    private readonly IOutfitService _outfitService;
    private readonly IOutfitGeneratedImageRepository _repository;
    private readonly IAiAssetStorageService _assetStorageService;

    public SaveUploadedOutfitGeneratedImage(
        IOutfitService outfitService,
        IOutfitGeneratedImageRepository repository,
        IAiAssetStorageService assetStorageService)
    {
        _outfitService = outfitService;
        _repository = repository;
        _assetStorageService = assetStorageService;
    }

    public async Task<OutfitGeneratedImageDto> ExecuteAsync(SaveUploadedOutfitGeneratedImageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourcePath) || !File.Exists(request.SourcePath))
            throw new InvalidOperationException("请选择一张有效的本地图片。");

        var outfit = await _outfitService.GetOutfitByIdAsync(request.OutfitId)
            ?? throw new InvalidOperationException("搭配不存在。");

        var bytes = await File.ReadAllBytesAsync(request.SourcePath);
        var mimeType = ResolveMimeType(request.SourcePath);
        var storedFileName = await _assetStorageService.SaveGeneratedImageAsync(bytes, mimeType);
        var existingImages = await _repository.GetByOutfitIdAsync(outfit.Id);

        var image = new OutfitGeneratedImage
        {
            OutfitId = outfit.Id,
            ProviderKind = "Manual Upload",
            Model = string.IsNullOrWhiteSpace(request.DisplayName) ? "手动上传" : request.DisplayName.Trim(),
            PromptSnapshot = request.Note?.Trim() ?? "用户手动上传效果图",
            ProfileSnapshotJson = string.Empty,
            OutfitSnapshotJson = string.Empty,
            OptionSnapshotJson = string.Empty,
            ResultImagePath = storedFileName,
            IsPrimary = !existingImages.Any(),
            Status = "Succeeded",
            FailureReason = null,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        if (image.IsPrimary)
            await _repository.ClearPrimaryAsync(outfit.Id);

        await _repository.AddAsync(image);
        return image.ToDto();
    }

    private static string ResolveMimeType(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".png" => "image/png",
            _ => "image/png"
        };
    }
}
