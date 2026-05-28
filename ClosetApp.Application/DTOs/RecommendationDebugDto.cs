using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public sealed record RecommendationDebugDto(
    string OutfitName,
    int TotalScore,
    int BaseScore,
    int SeasonScore,
    int FavoriteScore,
    int RecentWearScore,
    int WearCountScore,
    int SceneScore,
    int PreferenceSceneScore,
    int PreferenceTagScore,
    int PreferenceColorScore,
    IReadOnlyList<string> Reasons,
    IReadOnlyDictionary<OutfitScene, int> SceneWeights,
    IReadOnlyDictionary<string, int> TagWeights,
    IReadOnlyDictionary<string, int> ColorWeights,
    int TotalPreferenceWeight)
{
    public IReadOnlyList<ScoreBreakdownItem> Breakdown => BuildBreakdown();

    private IReadOnlyList<ScoreBreakdownItem> BuildBreakdown()
    {
        var items = new List<ScoreBreakdownItem>();

        items.Add(new ScoreBreakdownItem("基础评分", BaseScore, $"评分 {BaseScore / 12} 星 × 12"));
        AddIfNonZero(items, "季节匹配", SeasonScore, SeasonScore switch
        {
            >= 30 => "温度与季节完美匹配",
            >= 18 => "温度与季节较匹配",
            >= 0 => "季节匹配度一般",
            _ => "温度与季节不太匹配"
        });
        AddIfNonZero(items, "收藏标记", FavoriteScore, "被标记为收藏搭配");
        AddIfNonZero(items, "最近穿着", RecentWearScore, RecentWearScore switch
        {
            >= 10 => "很久没穿或从未穿过",
            <= -48 => "今天已经穿过",
            <= -28 => "最近穿过",
            _ => "穿着间隔适中"
        });
        AddIfNonZero(items, "穿着频次", WearCountScore, WearCountScore switch
        {
            >= 6 => "穿着次数较少，值得多穿",
            <= -8 => "穿着次数较多",
            _ => "穿着频次正常"
        });
        AddIfNonZero(items, "场景匹配", SceneScore, "场景与指定场景一致");
        AddIfNonZero(items, "偏好-场景", PreferenceSceneScore, "贴近常穿场景偏好");
        AddIfNonZero(items, "偏好-标签", PreferenceTagScore, "贴近常选标签偏好");
        AddIfNonZero(items, "偏好-颜色", PreferenceColorScore, "贴近常选颜色偏好");

        return items;
    }

    private static void AddIfNonZero(List<ScoreBreakdownItem> items, string label, int score, string detail)
    {
        if (score != 0)
            items.Add(new ScoreBreakdownItem(label, score, detail));
    }
}

public sealed record ScoreBreakdownItem(string Label, int Score, string Detail);
