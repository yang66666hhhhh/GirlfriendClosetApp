using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public sealed record RecommendationReadinessSummaryDto(
    string Title,
    string Detail,
    Season? MissingSeason,
    int MatchingSeasonCount,
    int ReadyOutfitCount)
{
    public bool HasGap => MissingSeason.HasValue || MatchingSeasonCount == 0;
}
