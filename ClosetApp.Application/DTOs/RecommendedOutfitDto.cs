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
    public bool IsWornToday => WornDate?.Date == DateTime.Today;
    public IList<Clothing> PreviewClothes => Outfit.OutfitClothes
        .Select(link => link.Clothing)
        .Where(clothing => clothing != null)
        .Cast<Clothing>()
        .ToList();
    public string WearSummaryText => WearCount > 0 ? $"已穿 {WearCount} 次" : "还没穿过";
    public string ReasonChipText => BuildReasonChipText();
    public string ReasonSummaryText => BuildReasonSummaryText();
    public string UserReasonHeadline => BuildUserReasonHeadline();
    public IReadOnlyList<string> HighlightTags => BuildHighlightTags();
    public IReadOnlyList<string> DisplayReasonTags => BuildDisplayReasonTags();
    public string? CautionText => BuildCautionText();

    private string BuildReasonChipText()
    {
        if (!WornDate.HasValue || WearCount == 0)
            return "适合今天试";

        if (IsWornToday)
            return "今天穿过";

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

    private string BuildUserReasonHeadline()
    {
        if (IsWornToday)
            return "今天已经穿过了，但它依然和当前条件匹配。";

        if (PrimaryReason.Contains("温度", StringComparison.OrdinalIgnoreCase) ||
            PrimaryReason.Contains("天气", StringComparison.OrdinalIgnoreCase))
        {
            return "今天的天气条件和这套搭配比较合拍。";
        }

        if (PrimaryReason.Contains("收藏", StringComparison.OrdinalIgnoreCase))
            return "这套是你明确偏爱的搭配，今天值得优先翻出来。";

        if (Reasons.Any(reason => reason.Contains("没穿", StringComparison.OrdinalIgnoreCase)))
            return "它已经有一阵子没出场了，今天穿会比较有新鲜感。";

        return "综合天气、穿着记录和偏好后，它是今天比较顺手的一套。";
    }

    private IReadOnlyList<string> BuildHighlightTags()
    {
        var tags = new List<string>();

        if (PrimaryReason.Contains("温度", StringComparison.OrdinalIgnoreCase) ||
            PrimaryReason.Contains("天气", StringComparison.OrdinalIgnoreCase) ||
            Reasons.Any(reason => reason.Contains("温度", StringComparison.OrdinalIgnoreCase)))
        {
            tags.Add("温度合适");
        }

        if (PrimaryReason.Contains("收藏", StringComparison.OrdinalIgnoreCase) ||
            Reasons.Any(reason => reason.Contains("收藏", StringComparison.OrdinalIgnoreCase)))
        {
            tags.Add("你的偏爱");
        }

        if (Reasons.Any(reason => reason.Contains("没穿", StringComparison.OrdinalIgnoreCase) ||
                                  reason.Contains("新鲜", StringComparison.OrdinalIgnoreCase)))
        {
            tags.Add("值得轮换");
        }

        if (Reasons.Any(reason => reason.Contains("场景", StringComparison.OrdinalIgnoreCase)))
            tags.Add("场景贴合");

        if (Reasons.Any(reason => reason.Contains("颜色", StringComparison.OrdinalIgnoreCase)))
            tags.Add("颜色顺手");

        if (tags.Count == 0)
            tags.Add("今天顺手");

        return tags.Take(3).ToList();
    }

    private IReadOnlyList<string> BuildDisplayReasonTags()
    {
        var tags = new List<string> { ReasonChipText };

        foreach (var tag in HighlightTags)
        {
            if (string.Equals(tag, "今天顺手", StringComparison.Ordinal) &&
                tags.Any(existing => string.Equals(existing, ReasonChipText, StringComparison.Ordinal)))
            {
                continue;
            }

            if (string.Equals(tag, WearSummaryText, StringComparison.Ordinal))
                continue;

            tags.Add(tag);
            if (tags.Count >= 3)
                break;
        }

        if (!tags.Any(tag => string.Equals(tag, WearSummaryText, StringComparison.Ordinal)))
            tags.Add(WearSummaryText);

        return tags.Take(3).ToList();
    }

    private string? BuildCautionText()
    {
        if (IsWornToday)
            return "今天已经记录过这套，除非你想重复穿。";

        if (WearCount >= 6)
            return "这套已经是高频搭配了，可以和别的轮换一下。";

        return null;
    }
}
