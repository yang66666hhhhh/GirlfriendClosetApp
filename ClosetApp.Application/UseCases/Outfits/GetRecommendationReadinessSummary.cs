using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.UseCases.Outfits;

public sealed class GetRecommendationReadinessSummary
{
    private readonly IOutfitService _outfitService;

    public GetRecommendationReadinessSummary(IOutfitService outfitService)
    {
        _outfitService = outfitService;
    }

    public async Task<RecommendationReadinessSummaryDto> ExecuteAsync(int temperature)
    {
        var outfits = (await _outfitService.GetAllOutfitsAsync()).ToList();
        return Build(outfits, temperature);
    }

    public static RecommendationReadinessSummaryDto Build(IReadOnlyCollection<Outfit> outfits, int temperature)
    {
        var readyOutfits = outfits
            .Where(outfit => outfit.OutfitClothes.Count > 0)
            .ToList();

        if (outfits.Count == 0)
        {
            return new RecommendationReadinessSummaryDto(
                "还没有搭配",
                "先建 2-3 套常穿组合，天气推荐就会马上有感觉。",
                null,
                0,
                0);
        }

        if (readyOutfits.Count == 0)
        {
            return new RecommendationReadinessSummaryDto(
                "搭配还缺衣物",
                "现有搭配里还没有可用于推荐的完整组合，先给常用搭配补上衣物。",
                null,
                0,
                0);
        }

        if (readyOutfits.All(outfit => outfit.Season == Season.Unspecified))
        {
            return new RecommendationReadinessSummaryDto(
                "搭配还没补季节",
                "先把常穿那几套标清楚春夏秋冬，推荐会准很多。",
                null,
                0,
                readyOutfits.Count);
        }

        var suggestedSeason = ResolveSuggestedSeason(temperature);
        var matchingCount = readyOutfits.Count(outfit =>
            outfit.Season == suggestedSeason ||
            outfit.Season == Season.AllSeason);

        if (matchingCount == 0)
        {
            return new RecommendationReadinessSummaryDto(
                $"缺少{GetSeasonName(suggestedSeason)}搭配",
                $"当前 {temperature}°C 左右更需要{GetSeasonName(suggestedSeason)}或四季搭配，补几套会更好用。",
                suggestedSeason,
                0,
                readyOutfits.Count);
        }

        return new RecommendationReadinessSummaryDto(
            "推荐准备好了",
            matchingCount >= 3
                ? $"当前温度已有 {matchingCount} 套可用搭配，可以放心轮换。"
                : $"当前温度已有 {matchingCount} 套可用搭配，再补几套会更耐穿。",
            null,
            matchingCount,
            readyOutfits.Count);
    }

    private static Season ResolveSuggestedSeason(int temperature)
    {
        return temperature switch
        {
            >= 26 => Season.Summer,
            >= 16 => Season.Spring,
            >= 10 => Season.Autumn,
            _ => Season.Winter
        };
    }

    private static string GetSeasonName(Season season)
    {
        return season switch
        {
            Season.Spring => "春季",
            Season.Summer => "夏季",
            Season.Autumn => "秋季",
            Season.Winter => "冬季",
            Season.AllSeason => "四季",
            _ => "当前"
        };
    }
}
