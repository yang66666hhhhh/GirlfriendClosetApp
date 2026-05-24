using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

// Carries ranking details so the UI can explain why an outfit surfaced today.
public sealed record RecommendedOutfitDto(
    Outfit Outfit,
    int Score,
    string PrimaryReason,
    string? SecondaryReason,
    IReadOnlyList<string> Reasons)
{
    public string Name => Outfit.Name;
    public Season Season => Outfit.Season;
    public OutfitScene Scene => Outfit.Scene;
    public int Rating => Outfit.Rating;
    public int WearCount => Outfit.WearCount;
    public DateTime? WornDate => Outfit.WornDate;
    public IList<Clothing> PreviewClothes => Outfit.OutfitClothes
        .Select(link => link.Clothing)
        .Where(clothing => clothing != null)
        .Cast<Clothing>()
        .ToList();
    public string WearSummaryText => WearCount > 0 ? $"已穿 {WearCount} 次" : "还没穿过";
    public string ReasonChipText => BuildReasonChipText();
    public string ReasonSummaryText => BuildReasonSummaryText();

    private string BuildReasonChipText()
    {
        if (!WornDate.HasValue || WearCount == 0)
            return "适合今天试";

        if (PrimaryReason.Contains("天气", StringComparison.OrdinalIgnoreCase) ||
            PrimaryReason.Contains("温度", StringComparison.OrdinalIgnoreCase))
            return "温度匹配";

        if (PrimaryReason.Contains("收藏", StringComparison.OrdinalIgnoreCase))
            return "优先翻出来";

        return WearCount >= 3 ? "顺手常穿" : "今日推荐";
    }

    private string BuildReasonSummaryText()
    {
        var source = string.IsNullOrWhiteSpace(SecondaryReason) ? PrimaryReason : SecondaryReason!;
        source = source.Trim();
        if (source.Length <= 26)
            return source;

        return $"{source[..26].TrimEnd('，', '。', ' ') }...";
    }
}
