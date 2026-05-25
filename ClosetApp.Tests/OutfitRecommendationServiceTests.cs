using ClosetApp.Application.Services;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitRecommendationServiceTests
{
    [Fact]
    public async Task GetRecommendationsByRuleAsync_HotWeather_PrefersSummerOutfit()
    {
        var summer = Outfit("Linen Set", Season.Summer, rating: 3);
        var winter = Outfit("Wool Coat", Season.Winter, rating: 5);
        var service = new OutfitRecommendationService(new FakeOutfitRepository([winter, summer]));

        var result = (await service.GetRecommendationsByRuleAsync(30)).ToList();

        Assert.Equal(summer, result[0].Outfit);
        Assert.Contains("季节感正合适", result[0].PrimaryReason);
    }

    [Fact]
    public async Task GetRecommendationsByRuleAsync_AllSeasonRanksAboveWrongSeason()
    {
        var allSeason = Outfit("Simple Dress", Season.AllSeason, rating: 3);
        var winter = Outfit("Heavy Coat", Season.Winter, rating: 3);
        var service = new OutfitRecommendationService(new FakeOutfitRepository([winter, allSeason]));

        var result = (await service.GetRecommendationsByRuleAsync(23)).ToList();

        Assert.Equal(allSeason, result[0].Outfit);
    }

    [Fact]
    public async Task GetRecommendationsByRuleAsync_RecentlyWornOutfitDropsInRank()
    {
        var recent = Outfit("Yesterday Look", Season.Spring, rating: 5, wornDate: DateTime.Today.AddDays(-1));
        var fresh = Outfit("Fresh Look", Season.Spring, rating: 4, wornDate: DateTime.Today.AddDays(-30));
        var service = new OutfitRecommendationService(new FakeOutfitRepository([recent, fresh]));

        var result = (await service.GetRecommendationsByRuleAsync(20)).ToList();

        Assert.Equal(fresh, result[0].Outfit);
    }

    [Fact]
    public async Task GetRecommendationsByRuleAsync_TodaysOutfitStronglyDropsInRank()
    {
        var today = Outfit("Today Look", Season.Spring, rating: 5, wornDate: DateTime.Today);
        var nextOption = Outfit("Next Look", Season.Spring, rating: 4, wornDate: DateTime.Today.AddDays(-12));
        var service = new OutfitRecommendationService(new FakeOutfitRepository([today, nextOption]));

        var result = (await service.GetRecommendationsByRuleAsync(20)).ToList();

        Assert.Equal(nextOption, result[0].Outfit);
        Assert.Contains(result.Single(item => item.Outfit == today).Reasons, reason => reason.Contains("今天已经穿过一次"));
    }

    [Fact]
    public async Task GetRecommendationsByRuleAsync_FavoriteHighRatedOutfitGetsBoost()
    {
        var plain = Outfit("Plain Look", Season.Autumn, rating: 3);
        var favorite = Outfit("Favorite Look", Season.Autumn, rating: 4, isFavorite: true);
        var service = new OutfitRecommendationService(new FakeOutfitRepository([plain, favorite]));

        var result = (await service.GetRecommendationsByRuleAsync(15)).ToList();

        Assert.Equal(favorite, result[0].Outfit);
        Assert.Contains("收藏", result[0].PrimaryReason + result[0].SecondaryReason);
    }

    [Fact]
    public async Task GetRecommendationsByRuleAsync_IgnoresEmptyOutfits()
    {
        var empty = Outfit("Empty", Season.Spring, rating: 5, withClothes: false);
        var complete = Outfit("Complete", Season.Spring, rating: 3);
        var service = new OutfitRecommendationService(new FakeOutfitRepository([empty, complete]));

        var result = (await service.GetRecommendationsByRuleAsync(20)).ToList();

        Assert.Single(result);
        Assert.Equal(complete, result[0].Outfit);
    }

    [Fact]
    public async Task GetRecommendationsByRuleAsync_LearnsScenePreferenceFromWearHistory()
    {
        var preferredHistory = Outfit("Often Casual", Season.Spring, rating: 3, scene: OutfitScene.Casual, wearCount: 6);
        var preferredCandidate = Outfit("Casual Candidate", Season.Spring, rating: 3, scene: OutfitScene.Casual);
        var otherCandidate = Outfit("Work Candidate", Season.Spring, rating: 3, scene: OutfitScene.Work);
        var service = new OutfitRecommendationService(new FakeOutfitRepository([otherCandidate, preferredCandidate, preferredHistory]));

        var result = (await service.GetRecommendationsByRuleAsync(20)).ToList();

        Assert.True(result.FindIndex(item => item.Outfit == preferredCandidate) < result.FindIndex(item => item.Outfit == otherCandidate));
        Assert.Contains(result.Single(item => item.Outfit == preferredCandidate).Reasons, reason => reason.Contains("常穿/收藏的场景"));
    }

    [Fact]
    public async Task GetRecommendationsByRuleAsync_LearnsTagAndColorPreference()
    {
        var preferredHistory = Outfit("Favorite Soft Look", Season.Spring, rating: 3, wearCount: 4, tagNames: ["温柔"], color: "奶白");
        var preferredCandidate = Outfit("Soft Candidate", Season.Spring, rating: 3, tagNames: ["温柔"], color: "奶白");
        var otherCandidate = Outfit("Sharp Candidate", Season.Spring, rating: 3, tagNames: ["酷感"], color: "黑色");
        var service = new OutfitRecommendationService(new FakeOutfitRepository([otherCandidate, preferredCandidate, preferredHistory]));

        var result = (await service.GetRecommendationsByRuleAsync(20)).ToList();

        Assert.True(result.FindIndex(item => item.Outfit == preferredCandidate) < result.FindIndex(item => item.Outfit == otherCandidate));
        var reasons = result.Single(item => item.Outfit == preferredCandidate).Reasons;
        Assert.Contains(reasons, reason => reason.Contains("风格标签"));
        Assert.Contains(reasons, reason => reason.Contains("颜色"));
    }

    private static Outfit Outfit(
        string name,
        Season season,
        int rating,
        DateTime? wornDate = null,
        bool isFavorite = false,
        bool withClothes = true,
        OutfitScene scene = OutfitScene.Casual,
        int? wearCount = null,
        IReadOnlyList<string>? tagNames = null,
        string? color = null)
    {
        var outfit = new Outfit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Scene = scene,
            Season = season,
            Rating = rating,
            WornDate = wornDate,
            WearCount = wearCount ?? (wornDate.HasValue ? 1 : 0)
        };

        if (withClothes)
        {
            var clothingId = Guid.NewGuid();
            var clothing = new Clothing
            {
                Id = clothingId,
                Name = $"{name} clothing",
                Color = color
            };

            foreach (var tagName in tagNames ?? [])
            {
                clothing.ClothingTags.Add(new ClothingTag
                {
                    ClothingId = clothingId,
                    Clothing = clothing,
                    TagId = Guid.NewGuid(),
                    Tag = new Tag { Name = tagName }
                });
            }

            outfit.OutfitClothes.Add(new OutfitClothing
            {
                OutfitId = outfit.Id,
                ClothingId = clothingId,
                Clothing = clothing
            });
        }

        if (isFavorite)
        {
            outfit.Favorites.Add(new Favorite
            {
                OutfitId = outfit.Id
            });
        }

        return outfit;
    }

    private sealed class FakeOutfitRepository : IOutfitRepository
    {
        private readonly IReadOnlyList<Outfit> _outfits;

        public FakeOutfitRepository(IReadOnlyList<Outfit> outfits)
        {
            _outfits = outfits;
        }

        public Task<IEnumerable<Outfit>> GetAllAsync() => Task.FromResult(_outfits.AsEnumerable());
        public Task<Outfit?> GetByIdAsync(Guid id) => Task.FromResult(_outfits.FirstOrDefault(outfit => outfit.Id == id));
        public Task AddAsync(Outfit entity) => throw new NotImplementedException();
        public Task UpdateAsync(Outfit entity) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<Outfit>> GetBySceneAsync(OutfitScene scene) => Task.FromResult(_outfits.Where(outfit => outfit.Scene == scene));
        public Task<IEnumerable<Outfit>> GetBySeasonAsync(Season season) => Task.FromResult(_outfits.Where(outfit => outfit.Season == season));
        public Task<IEnumerable<Outfit>> GetRecentlyWornAsync(int count) => Task.FromResult(_outfits.Where(outfit => outfit.WornDate.HasValue).Take(count));
        public Task DeleteEmptyOutfitsAsync() => throw new NotImplementedException();
    }
}
