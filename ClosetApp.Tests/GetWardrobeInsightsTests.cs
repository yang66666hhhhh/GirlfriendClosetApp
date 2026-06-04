using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Insights;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using Xunit;

namespace ClosetApp.Tests;

public class GetWardrobeInsightsTests
{
    [Fact]
    public async Task ExecuteAsync_EmptyOutfits_ReturnsZeroStats()
    {
        var service = new FakeOutfitService([]);
        var useCase = new GetWardrobeInsights(service);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(0, result.TotalOutfitCount);
        Assert.Equal(0, result.WornOutfitCount);
        Assert.Equal(0, result.NeverWornCount);
        Assert.Equal(0, result.WearRate);
        Assert.Equal(0, result.TotalWearCount);
        Assert.Equal(0, result.ActiveDays);
        Assert.Equal(0, result.CurrentStreak);
        Assert.Empty(result.TopWornOutfits);
        Assert.Empty(result.IdleOutfits);
    }

    [Fact]
    public async Task ExecuteAsync_WithOutfits_CalculatesWearRate()
    {
        var worn1 = CreateOutfit("搭配1", wearCount: 3, wornDate: DateTime.Today);
        var worn2 = CreateOutfit("搭配2", wearCount: 1, wornDate: DateTime.Today.AddDays(-5));
        var never = CreateOutfit("搭配3", wearCount: 0);
        var service = new FakeOutfitService([worn1, worn2, never]);
        var useCase = new GetWardrobeInsights(service);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(3, result.TotalOutfitCount);
        Assert.Equal(2, result.WornOutfitCount);
        Assert.Equal(1, result.NeverWornCount);
        Assert.Equal(66, result.WearRate);
        Assert.Equal(4, result.TotalWearCount);
    }

    [Fact]
    public async Task ExecuteAsync_TopWornOutfits_ReturnsTopFive()
    {
        var outfits = Enumerable.Range(1, 8)
            .Select(i => CreateOutfit($"搭配{i}", wearCount: i, wornDate: DateTime.Today.AddDays(-i)))
            .ToList();
        var service = new FakeOutfitService(outfits);
        var useCase = new GetWardrobeInsights(service);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(5, result.TopWornOutfits.Count);
        Assert.Equal("搭配8", result.TopWornOutfits[0].Name);
        Assert.Equal(8, result.TopWornOutfits[0].WearCount);
        Assert.Equal("搭配4", result.TopWornOutfits[4].Name);
        Assert.Equal(4, result.TopWornOutfits[4].WearCount);
    }

    [Fact]
    public async Task ExecuteAsync_SceneDistribution_GroupsByScene()
    {
        var work1 = CreateOutfit("通勤1", scene: OutfitScene.Work, wearCount: 1);
        var work2 = CreateOutfit("通勤2", scene: OutfitScene.Work, wearCount: 1);
        var date = CreateOutfit("约会", scene: OutfitScene.Date, wearCount: 1);
        var service = new FakeOutfitService([work1, work2, date]);
        var useCase = new GetWardrobeInsights(service);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(2, result.SceneDistribution.Count);
        Assert.Equal("通勤", result.SceneDistribution[0].Label);
        Assert.Equal(2, result.SceneDistribution[0].Count);
        Assert.Equal("约会", result.SceneDistribution[1].Label);
        Assert.Equal(1, result.SceneDistribution[1].Count);
    }

    [Fact]
    public async Task ExecuteAsync_SeasonDistribution_GroupsBySeason()
    {
        var summer = CreateOutfit("夏季", season: Season.Summer, wearCount: 1);
        var winter = CreateOutfit("冬季", season: Season.Winter, wearCount: 1);
        var service = new FakeOutfitService([summer, winter]);
        var useCase = new GetWardrobeInsights(service);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(2, result.SeasonDistribution.Count);
        Assert.Contains(result.SeasonDistribution, d => d.Label == "夏季" && d.Count == 1);
        Assert.Contains(result.SeasonDistribution, d => d.Label == "冬季" && d.Count == 1);
    }

    [Fact]
    public async Task ExecuteAsync_IdleOutfits_FiltersByThreshold()
    {
        var recent = CreateOutfit("最近穿过", wearCount: 2, wornDate: DateTime.Today.AddDays(-5));
        var idle = CreateOutfit("闲置搭配", wearCount: 3, wornDate: DateTime.Today.AddDays(-30));
        var service = new FakeOutfitService([recent, idle]);
        var useCase = new GetWardrobeInsights(service);

        var result = await useCase.ExecuteAsync();

        Assert.Single(result.IdleOutfits);
        Assert.Equal("闲置搭配", result.IdleOutfits[0].Name);
        Assert.Equal(30, result.IdleOutfits[0].DaysSinceLastWorn);
    }

    [Fact]
    public async Task ExecuteAsync_CurrentStreak_CalculatesConsecutiveDays()
    {
        var today = CreateOutfit("今天", wearCount: 1, wornDate: DateTime.Today);
        var yesterday = CreateOutfit("昨天", wearCount: 1, wornDate: DateTime.Today.AddDays(-1));
        var twoDaysAgo = CreateOutfit("前天", wearCount: 1, wornDate: DateTime.Today.AddDays(-2));
        var service = new FakeOutfitService([today, yesterday, twoDaysAgo]);
        var useCase = new GetWardrobeInsights(service);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(3, result.CurrentStreak);
    }

    [Fact]
    public async Task ExecuteAsync_NoWornToday_StreakIsZero()
    {
        var yesterday = CreateOutfit("昨天", wearCount: 1, wornDate: DateTime.Today.AddDays(-1));
        var service = new FakeOutfitService([yesterday]);
        var useCase = new GetWardrobeInsights(service);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(0, result.CurrentStreak);
    }

    [Fact]
    public async Task ExecuteAsync_ActiveDays_CountsDistinctDates()
    {
        var outfit1 = CreateOutfit("搭配1", wearCount: 2, wornDate: DateTime.Today);
        var outfit2 = CreateOutfit("搭配2", wearCount: 1, wornDate: DateTime.Today);
        var outfit3 = CreateOutfit("搭配3", wearCount: 1, wornDate: DateTime.Today.AddDays(-1));
        var service = new FakeOutfitService([outfit1, outfit2, outfit3]);
        var useCase = new GetWardrobeInsights(service);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(2, result.ActiveDays);
    }

    private static Outfit CreateOutfit(
        string name,
        OutfitScene scene = OutfitScene.Casual,
        Season season = Season.AllSeason,
        int wearCount = 0,
        DateTime? wornDate = null)
    {
        return new Outfit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Scene = scene,
            Season = season,
            WearCount = wearCount,
            WornDate = wornDate,
            CreatedAt = DateTime.Today
        };
    }

    private sealed class FakeOutfitService : IOutfitService
    {
        private readonly List<Outfit> _outfits;

        public FakeOutfitService(IReadOnlyList<Outfit> outfits)
        {
            _outfits = outfits.ToList();
        }

        public Task<IEnumerable<Outfit>> GetAllOutfitsAsync() => Task.FromResult(_outfits.AsEnumerable());
        public Task<Outfit?> GetOutfitByIdAsync(Guid id) => Task.FromResult(_outfits.FirstOrDefault(o => o.Id == id));
        public Task<Outfit> AddOutfitAsync(Outfit outfit) => throw new NotImplementedException();
        public Task UpdateOutfitAsync(Outfit outfit) => throw new NotImplementedException();
        public Task DeleteOutfitAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<Outfit>> GetOutfitsBySceneAsync(OutfitScene scene) => throw new NotImplementedException();
        public Task<IEnumerable<Outfit>> GetRecentlyWornOutfitsAsync(int count) => throw new NotImplementedException();
        public Task<IEnumerable<OutfitWornRecord>> GetRecentWornRecordsAsync(int count) => throw new NotImplementedException();
        public Task<IEnumerable<OutfitWornRecord>> GetWornRecordsAsync(DateTime start, DateTime end) => throw new NotImplementedException();
        public Task RecordWornDateAsync(Guid outfitId, DateTime date) => throw new NotImplementedException();
        public Task<WornRecordImageHealthDto> AnalyzeWornRecordImageHealthAsync() => Task.FromResult(new WornRecordImageHealthDto(0, 0, 0, 0));
        public Task RepairWornRecordSnapshotImageAsync(Guid recordId, Guid clothingId, string imagePath) => throw new NotImplementedException();
        public Task DeleteWornRecordAsync(Guid recordId) => throw new NotImplementedException();
        public Task<bool> ToggleFavoriteAsync(Guid outfitId) => throw new NotImplementedException();
        public Task<IReadOnlyList<OutfitGeneratedImage>> GetGeneratedImagesAsync(Guid outfitId) => Task.FromResult<IReadOnlyList<OutfitGeneratedImage>>([]);
    }
}
