using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public sealed record TodayRecommendationResult(
    string City,
    int Temperature,
    string Condition,
    bool IsWeatherFromApi,
    IReadOnlyList<RecommendedOutfitDto> Recommendations,
    RecommendationReadinessSummaryDto? Readiness,
    string StatusText);