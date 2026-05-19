using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.UseCases.Clothing;

public sealed class CompleteClothingMetadataBatch
{
    private readonly IClothingRepository _clothingRepository;

    public CompleteClothingMetadataBatch(IClothingRepository clothingRepository)
    {
        _clothingRepository = clothingRepository;
    }

    public async Task<BatchClothingCompletionResult> ExecuteAsync(BatchClothingCompletionRequest request)
    {
        if (request.ClothingIds.Count == 0)
            throw new InvalidOperationException("当前没有可补全的衣服。");

        if (!HasAnyRequestedChange(request))
            throw new InvalidOperationException("请至少选择一项要补全的信息。");

        var updatedCount = 0;
        foreach (var clothingId in request.ClothingIds.Distinct())
        {
            var clothing = await _clothingRepository.GetByIdAsync(clothingId);
            if (clothing == null)
                continue;

            // Batch completion is conservative: only fill blanks, and only add missing tags.
            if (!ApplyMissingMetadata(clothing, request))
                continue;

            clothing.UpdatedAt = DateTime.Now;
            await _clothingRepository.UpdateAsync(clothing);
            updatedCount++;
        }

        return new BatchClothingCompletionResult(
            updatedCount,
            request.ClothingIds.Count - updatedCount);
    }

    private static bool ApplyMissingMetadata(
        global::ClosetApp.Domain.Entities.Clothing clothing,
        BatchClothingCompletionRequest request)
    {
        var changed = false;

        if (request.Type.HasValue && clothing.Type == ClothingType.Unspecified)
        {
            clothing.Type = request.Type.Value;
            changed = true;
        }

        if (request.Season.HasValue && clothing.Season == Season.Unspecified)
        {
            clothing.Season = request.Season.Value;
            changed = true;
        }

        var normalizedColor = Normalize(request.Color);
        if (normalizedColor != null && string.IsNullOrWhiteSpace(clothing.Color))
        {
            clothing.Color = normalizedColor;
            changed = true;
        }

        var normalizedBrand = Normalize(request.Brand);
        if (normalizedBrand != null && string.IsNullOrWhiteSpace(clothing.Brand))
        {
            clothing.Brand = normalizedBrand;
            changed = true;
        }

        var existingTagIds = clothing.ClothingTags.Select(tag => tag.TagId).ToHashSet();
        foreach (var tagId in request.TagIds.Distinct().Where(tagId => !existingTagIds.Contains(tagId)))
        {
            clothing.ClothingTags.Add(new ClothingTag
            {
                ClothingId = clothing.Id,
                TagId = tagId
            });
            changed = true;
        }

        return changed;
    }

    private static bool HasAnyRequestedChange(BatchClothingCompletionRequest request)
    {
        return request.Type.HasValue ||
            request.Season.HasValue ||
            !string.IsNullOrWhiteSpace(request.Color) ||
            !string.IsNullOrWhiteSpace(request.Brand) ||
            request.TagIds.Count > 0;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
