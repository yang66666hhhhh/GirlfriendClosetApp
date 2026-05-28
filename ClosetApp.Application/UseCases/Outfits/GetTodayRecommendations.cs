using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.UseCases.Outfits;

public sealed class GetTodayRecommendations
{
    private readonly IOutfitRecommendationService _recommendationService;
    private readonly GetRecommendationReadinessSummary _getReadinessSummary;

    public GetTodayRecommendations(
        IOutfitRecommendationService recommendationService,
        GetRecommendationReadinessSummary getReadinessSummary)
    {
        _recommendationService = recommendationService;
        _getReadinessSummary = getReadinessSummary;
    }

    public async Task<TodayRecommendationResult> ExecuteAsync(TodayRecommendationRequest request)
    {
        var recommendations = (await _recommendationService.GetRecommendationsByRuleAsync(
            request.Temperature,
            request.DefaultScene)).ToList();

        if (request.AvoidWornToday)
            recommendations = recommendations.Where(r => !r.IsWornToday).ToList();

        recommendations = ApplyRotationStrategy(recommendations, request.RotationStrategy)
            .Take(3)
            .ToList();

        var readiness = await _getReadinessSummary.ExecuteAsync(request.Temperature);
        var statusText = BuildStatusText(recommendations.Count, request.IsWeatherFromApi, request.City);

        return new TodayRecommendationResult(
            request.City,
            request.Temperature,
            request.Condition,
            request.IsWeatherFromApi,
            recommendations,
            readiness,
            statusText);
    }

    private static string BuildStatusText(int recommendationCount, bool isWeatherFromApi, string city)
    {
        if (recommendationCount > 0 && isWeatherFromApi)
            return "已按当前天气刷新推荐。";
        if (recommendationCount > 0)
            return "天气暂时缺席，但我还是先按体感温度帮你挑了几套。";
        if (isWeatherFromApi)
            return "天气已刷新，但衣橱里还没有匹配出来的搭配。";
        return $"暂时拿不到 {city} 的天气，先按季节体感继续推荐。";
    }

    private static IReadOnlyList<RecommendedOutfitDto> ApplyRotationStrategy(
        IReadOnlyList<RecommendedOutfitDto> recommendations,
        RecommendationRotationStrategy strategy)
    {
        return strategy switch
        {
            RecommendationRotationStrategy.PreferLessWorn => recommendations
                .OrderBy(r => r.WearCount)
                .ThenBy(r => r.WornDate ?? DateTime.MinValue)
                .ThenByDescending(r => r.Score)
                .ToList(),
            RecommendationRotationStrategy.PreferFavorites => recommendations
                .OrderByDescending(r => r.Outfit.Favorites.Count > 0)
                .ThenByDescending(r => r.Score)
                .ToList(),
            _ => recommendations
        };
    }
}

public sealed record TodayRecommendationRequest(
    string City,
    int Temperature,
    string Condition,
    bool IsWeatherFromApi,
    OutfitScene? DefaultScene,
    bool AvoidWornToday,
    RecommendationRotationStrategy RotationStrategy);