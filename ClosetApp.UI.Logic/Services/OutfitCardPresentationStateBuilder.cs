using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.Logic.Services;

public enum OutfitCardVisualMode
{
    OutfitPreview = 0,
    EffectImage = 1
}

public sealed record OutfitCardPresentationState(
    OutfitCardVisualMode VisualMode,
    string HintText,
    string? EffectImagePath,
    bool IsPrimaryImage,
    bool IsFallbackToOutfitPreview,
    bool HasSucceededEffectImage,
    bool HasFailedAttempt);

public static class OutfitCardPresentationStateBuilder
{
    public static OutfitCardPresentationState Build(Outfit outfit, OutfitCardDisplayMode displayMode)
    {
        var succeededImages = outfit.GeneratedImages
            .Where(image =>
                string.Equals(image.Status, "Succeeded", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(image.ResultImagePath))
            .OrderByDescending(image => image.IsPrimary)
            .ThenByDescending(image => image.CreatedAt)
            .ToList();
        var preferredImage = succeededImages.FirstOrDefault(image => image.IsPrimary) ?? succeededImages.FirstOrDefault();
        var hasFailedAttempt = outfit.GeneratedImages.Any(image =>
            string.Equals(image.Status, "Failed", StringComparison.OrdinalIgnoreCase));

        if (displayMode == OutfitCardDisplayMode.OutfitFirst)
        {
            return new OutfitCardPresentationState(
                OutfitCardVisualMode.OutfitPreview,
                succeededImages.Count > 0
                    ? "查看效果图"
                    : "上传效果图",
                null,
                false,
                false,
                succeededImages.Count > 0,
                hasFailedAttempt);
        }

        if (preferredImage?.ResultImagePath is { Length: > 0 } resultImagePath)
        {
            return new OutfitCardPresentationState(
                OutfitCardVisualMode.EffectImage,
                preferredImage.IsPrimary
                    ? "首选效果图"
                    : "历史效果图",
                resultImagePath,
                preferredImage.IsPrimary,
                false,
                true,
                hasFailedAttempt);
        }

        return new OutfitCardPresentationState(
            OutfitCardVisualMode.OutfitPreview,
            hasFailedAttempt
                ? "生成失败"
                : "等待效果图",
            null,
            false,
            true,
            false,
            hasFailedAttempt);
    }
}
