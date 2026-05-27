using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.ViewModels;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitsViewModelTests
{
    [Fact]
    public async Task ClearFiltersCommand_ResetsBoundFilters()
    {
        var target = CreateOutfit("豆沙通勤", OutfitScene.Work, Season.Autumn, isFavorite: true);
        var excluded = CreateOutfit("周末约会", OutfitScene.Date, Season.Spring);
        var viewModel = CreateViewModel([target, excluded]);
        await viewModel.LoadOutfitsAsync();

        viewModel.SearchText = "豆沙";
        viewModel.SelectedScene = OutfitScene.Work;
        viewModel.SelectedSeason = Season.Autumn;
        viewModel.FavoriteOnly = true;

        viewModel.ClearFiltersCommand.Execute(null);

        Assert.Null(viewModel.SelectedScene);
        Assert.Null(viewModel.SelectedSeason);
        Assert.False(viewModel.FavoriteOnly);
        Assert.Equal(string.Empty, viewModel.SearchText);
        Assert.Equal(2, viewModel.OutfitCount);
        Assert.Equal("全部搭配", viewModel.FilterSummary);
    }

    [Fact]
    public async Task RefreshWeatherRecommendationsWithFeedbackCommand_UsesWeatherAndKeepsTopThreeRecommendations()
    {
        var outfits = new[]
        {
            CreateOutfit("一号", OutfitScene.Casual, Season.Spring, withClothes: true),
            CreateOutfit("二号", OutfitScene.Work, Season.Spring, withClothes: true),
            CreateOutfit("三号", OutfitScene.Date, Season.Spring, withClothes: true),
            CreateOutfit("四号", OutfitScene.Travel, Season.Spring, withClothes: true)
        };
        var weather = new FakeWeatherService(new WeatherInfo
        {
            City = "Hangzhou",
            Temperature = 18,
            Condition = "多云"
        });
        var viewModel = CreateViewModel(outfits, weatherService: weather);

        await viewModel.RefreshWeatherRecommendationsWithFeedbackCommand.ExecuteAsync(null);

        Assert.Equal("Hangzhou", viewModel.WeatherCity);
        Assert.Equal(18, viewModel.WeatherTemperature);
        Assert.Equal("多云", viewModel.WeatherCondition);
        Assert.Equal(3, viewModel.WeatherRecommendations.Count);
        Assert.True(viewModel.HasPrimaryWeatherRecommendation);
        Assert.Equal("已按当前天气刷新推荐。", viewModel.WeatherStatusText);
        Assert.Equal("Shanghai", weather.RequestedCity);
    }

    [Fact]
    public async Task RefreshWeatherRecommendationsAsync_UsesPreferredScene()
    {
        var work = CreateOutfit("通勤搭配", OutfitScene.Work, Season.Spring, withClothes: true);
        var date = CreateOutfit("约会搭配", OutfitScene.Date, Season.Spring, withClothes: true);
        var recommendationService = new FakeRecommendationService([work, date]);
        var preferences = new FakeRecommendationPreferencesService(new RecommendationPreferences
        {
            DefaultScene = OutfitScene.Work,
            AvoidWornToday = false
        });
        var viewModel = CreateViewModel([work, date], recommendationPreferencesService: preferences, recommendationService: recommendationService);

        await viewModel.RefreshWeatherRecommendationsAsync();

        Assert.Equal(OutfitScene.Work, recommendationService.RequestedScene);
        var result = Assert.Single(viewModel.WeatherRecommendations);
        Assert.Equal("通勤搭配", result.Name);
    }

    [Fact]
    public async Task RefreshWeatherRecommendationsAsync_WhenEnabled_AvoidsWornToday()
    {
        var wornToday = CreateOutfit("今天穿过", OutfitScene.Work, Season.Spring, withClothes: true);
        wornToday.WornDate = DateTime.Today;
        wornToday.WearCount = 1;
        var fresh = CreateOutfit("还没穿", OutfitScene.Work, Season.Spring, withClothes: true);
        var preferences = new FakeRecommendationPreferencesService(new RecommendationPreferences
        {
            AvoidWornToday = true
        });
        var viewModel = CreateViewModel([wornToday, fresh], recommendationPreferencesService: preferences);

        await viewModel.RefreshWeatherRecommendationsAsync();

        var result = Assert.Single(viewModel.WeatherRecommendations);
        Assert.Equal("还没穿", result.Name);
    }

    [Fact]
    public async Task RefreshWeatherRecommendationsAsync_WithBalancedStrategy_KeepsRecommendationOrder()
    {
        var highWear = CreateOutfit("高分常穿", OutfitScene.Work, Season.Spring, withClothes: true);
        highWear.WearCount = 8;
        var lowWear = CreateOutfit("低分少穿", OutfitScene.Work, Season.Spring, withClothes: true);
        lowWear.WearCount = 0;
        var preferences = new FakeRecommendationPreferencesService(new RecommendationPreferences
        {
            RotationStrategy = RecommendationRotationStrategy.Balanced
        });
        var viewModel = CreateViewModel([highWear, lowWear], recommendationPreferencesService: preferences);

        await viewModel.RefreshWeatherRecommendationsAsync();

        Assert.Equal(["高分常穿", "低分少穿"], viewModel.WeatherRecommendations.Select(item => item.Name));
    }

    [Fact]
    public async Task RefreshWeatherRecommendationsAsync_WithPreferLessWornStrategy_PrioritizesLowWearCount()
    {
        var highWear = CreateOutfit("常穿", OutfitScene.Work, Season.Spring, withClothes: true);
        highWear.WearCount = 8;
        var lowWear = CreateOutfit("少穿", OutfitScene.Work, Season.Spring, withClothes: true);
        lowWear.WearCount = 0;
        var preferences = new FakeRecommendationPreferencesService(new RecommendationPreferences
        {
            RotationStrategy = RecommendationRotationStrategy.PreferLessWorn
        });
        var viewModel = CreateViewModel([highWear, lowWear], recommendationPreferencesService: preferences);

        await viewModel.RefreshWeatherRecommendationsAsync();

        Assert.Equal("少穿", viewModel.WeatherRecommendations[0].Name);
    }

    [Fact]
    public async Task RefreshWeatherRecommendationsAsync_WithPreferFavoritesStrategy_PrioritizesFavorites()
    {
        var plain = CreateOutfit("普通搭配", OutfitScene.Work, Season.Spring, withClothes: true);
        var favorite = CreateOutfit("收藏搭配", OutfitScene.Work, Season.Spring, isFavorite: true, withClothes: true);
        var preferences = new FakeRecommendationPreferencesService(new RecommendationPreferences
        {
            RotationStrategy = RecommendationRotationStrategy.PreferFavorites
        });
        var viewModel = CreateViewModel([plain, favorite], recommendationPreferencesService: preferences);

        await viewModel.RefreshWeatherRecommendationsAsync();

        Assert.Equal("收藏搭配", viewModel.WeatherRecommendations[0].Name);
    }

    [Fact]
    public async Task RecordRecommendedOutfitWornCommand_RecordsOutfitAndRefreshesState()
    {
        var outfit = CreateOutfit("今日通勤", OutfitScene.Work, Season.Spring, withClothes: true);
        var outfitService = new FakeOutfitService([outfit]);
        var viewModel = CreateViewModel([outfit], outfitService: outfitService);
        var recommendation = new RecommendedOutfitDto(outfit, 10, "适合今天", null, ["适合今天"]);

        await viewModel.RecordRecommendedOutfitWornCommand.ExecuteAsync(recommendation);

        Assert.Equal(outfit.Id, outfitService.RecordedOutfitId);
        Assert.NotNull(outfitService.RecordedDate);
        Assert.Single(viewModel.RecentWornRecords);
        Assert.True(viewModel.HasTodayWornRecords);
    }

    [Fact]
    public async Task DeleteOutfitWithFeedbackAsync_DeletesOutfitAndRefreshesList()
    {
        var removed = CreateOutfit("旧搭配", OutfitScene.Work, Season.Autumn);
        var kept = CreateOutfit("保留搭配", OutfitScene.Casual, Season.Spring);
        var outfitService = new FakeOutfitService([removed, kept]);
        var viewModel = CreateViewModel([removed, kept], outfitService: outfitService);
        await viewModel.LoadOutfitsAsync();

        await viewModel.DeleteOutfitWithFeedbackAsync(removed);

        Assert.Equal(removed.Id, outfitService.DeletedOutfitId);
        Assert.Single(viewModel.Outfits);
        Assert.Equal("保留搭配", viewModel.Outfits[0].Name);
    }

    [Fact]
    public async Task ToggleFavoriteWithFeedbackAsync_TogglesFavoriteAndRefreshesList()
    {
        var outfit = CreateOutfit("常穿搭配", OutfitScene.Work, Season.Autumn);
        var outfitService = new FakeOutfitService([outfit]);
        var viewModel = CreateViewModel([outfit], outfitService: outfitService);

        var result = await viewModel.ToggleFavoriteWithFeedbackAsync(outfit);

        Assert.True(result);
        Assert.Equal(outfit.Id, outfitService.ToggledFavoriteOutfitId);
        Assert.True(viewModel.Outfits[0].Favorites.Count > 0);
    }

    [Fact]
    public async Task RefreshAfterOutfitSavedWithFeedbackAsync_ReloadsOutfits()
    {
        var first = CreateOutfit("原有搭配", OutfitScene.Work, Season.Autumn);
        var outfitService = new FakeOutfitService([first]);
        var viewModel = CreateViewModel([first], outfitService: outfitService);
        await viewModel.LoadOutfitsAsync();

        outfitService.AddStoredOutfit(CreateOutfit("新搭配", OutfitScene.Casual, Season.Spring));

        await viewModel.RefreshAfterOutfitSavedWithFeedbackAsync("已保存搭配", "新的搭配已经出现在列表里。");

        Assert.Equal(2, viewModel.OutfitCount);
        Assert.Contains(viewModel.Outfits, outfit => outfit.Name == "新搭配");
    }

    private static OutfitsViewModel CreateViewModel(
        IReadOnlyList<Outfit> outfits,
        FakeOutfitService? outfitService = null,
        IWeatherService? weatherService = null,
        FakeRecommendationPreferencesService? recommendationPreferencesService = null,
        FakeRecommendationService? recommendationService = null)
    {
        var resolvedOutfitService = outfitService ?? new FakeOutfitService(outfits);
        return new OutfitsViewModel(
            resolvedOutfitService,
            recommendationService ?? new FakeRecommendationService(outfits),
            weatherService ?? new FakeWeatherService(null),
            new FakeWeatherPreferencesService("Shanghai"),
            recommendationPreferencesService ?? new FakeRecommendationPreferencesService(),
            new GetRecommendationReadinessSummary(resolvedOutfitService));
    }

    private static Outfit CreateOutfit(
        string name,
        OutfitScene scene,
        Season season,
        bool isFavorite = false,
        bool withClothes = false)
    {
        var outfit = new Outfit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Scene = scene,
            Season = season,
            CreatedAt = DateTime.Today
        };

        if (isFavorite)
            outfit.Favorites.Add(new Favorite { OutfitId = outfit.Id });

        if (withClothes)
        {
            var clothing = new Clothing
            {
                Id = Guid.NewGuid(),
                Name = $"{name} 单品"
            };
            outfit.OutfitClothes.Add(new OutfitClothing
            {
                OutfitId = outfit.Id,
                ClothingId = clothing.Id,
                Clothing = clothing
            });
        }

        return outfit;
    }

    private sealed class FakeOutfitService : IOutfitService
    {
        private readonly List<Outfit> _outfits;
        private readonly List<OutfitWornRecord> _records = [];

        public FakeOutfitService(IReadOnlyList<Outfit> outfits)
        {
            _outfits = outfits.ToList();
        }

        public Guid? RecordedOutfitId { get; private set; }
        public DateTime? RecordedDate { get; private set; }
        public Guid? DeletedOutfitId { get; private set; }
        public Guid? ToggledFavoriteOutfitId { get; private set; }

        public Task<IEnumerable<Outfit>> GetAllOutfitsAsync() => Task.FromResult(_outfits.AsEnumerable());
        public Task<Outfit?> GetOutfitByIdAsync(Guid id) => Task.FromResult(_outfits.FirstOrDefault(outfit => outfit.Id == id));
        public Task<Outfit> AddOutfitAsync(Outfit outfit) => throw new NotImplementedException();
        public Task UpdateOutfitAsync(Outfit outfit) => throw new NotImplementedException();
        public Task DeleteOutfitAsync(Guid id)
        {
            DeletedOutfitId = id;
            _outfits.RemoveAll(outfit => outfit.Id == id);
            return Task.CompletedTask;
        }
        public Task<IEnumerable<Outfit>> GetOutfitsBySceneAsync(OutfitScene scene) => Task.FromResult(_outfits.Where(outfit => outfit.Scene == scene));
        public Task<IEnumerable<Outfit>> GetRecentlyWornOutfitsAsync(int count) => Task.FromResult(_outfits.Where(outfit => outfit.WornDate.HasValue).Take(count));
        public Task<IEnumerable<OutfitWornRecord>> GetRecentWornRecordsAsync(int count) => Task.FromResult(_records.OrderByDescending(record => record.WornDate).Take(count).AsEnumerable());
        public Task<IEnumerable<OutfitWornRecord>> GetWornRecordsAsync(DateTime start, DateTime end)
        {
            return Task.FromResult(_records
                .Where(record => record.WornDate >= start && record.WornDate <= end)
                .AsEnumerable());
        }

        public Task RecordWornDateAsync(Guid outfitId, DateTime date)
        {
            var outfit = _outfits.First(outfit => outfit.Id == outfitId);
            outfit.WornDate = date;
            outfit.WearCount++;
            RecordedOutfitId = outfitId;
            RecordedDate = date;
            _records.Add(new OutfitWornRecord
            {
                Id = Guid.NewGuid(),
                OutfitId = outfitId,
                Outfit = outfit,
                WornDate = date
            });
            return Task.CompletedTask;
        }

        public Task DeleteWornRecordAsync(Guid recordId) => throw new NotImplementedException();
        public Task<bool> ToggleFavoriteAsync(Guid outfitId)
        {
            var outfit = _outfits.First(outfit => outfit.Id == outfitId);
            ToggledFavoriteOutfitId = outfitId;

            if (outfit.Favorites.Count > 0)
            {
                outfit.Favorites.Clear();
                return Task.FromResult(false);
            }

            outfit.Favorites.Add(new Favorite { OutfitId = outfitId });
            return Task.FromResult(true);
        }

        public void AddStoredOutfit(Outfit outfit)
        {
            _outfits.Add(outfit);
        }
    }

    private sealed class FakeRecommendationService : IOutfitRecommendationService
    {
        private readonly IReadOnlyList<Outfit> _outfits;

        public FakeRecommendationService(IReadOnlyList<Outfit> outfits)
        {
            _outfits = outfits;
        }

        public OutfitScene? RequestedScene { get; private set; }

        public Task<RecommendedOutfitDto?> GetRecommendationAsync(int temperature, OutfitScene? scene = null)
        {
            return Task.FromResult(GetRecommendations(temperature, scene).FirstOrDefault());
        }

        public Task<IEnumerable<RecommendedOutfitDto>> GetRecommendationsByRuleAsync(int temperature, OutfitScene? scene = null)
        {
            RequestedScene = scene;
            return Task.FromResult(GetRecommendations(temperature, scene).AsEnumerable());
        }

        public Task<IEnumerable<Outfit>> GetLowWearOutfitsAsync(int count = 5) => throw new NotImplementedException();
        public Task<IEnumerable<Outfit>> GetUnwornOutfitsAsync() => throw new NotImplementedException();

        private List<RecommendedOutfitDto> GetRecommendations(int temperature, OutfitScene? scene)
        {
            return _outfits
                .Where(outfit => scene == null || outfit.Scene == scene)
                .Select((outfit, index) => new RecommendedOutfitDto(
                    outfit,
                    100 - index,
                    $"按 {temperature}°C 推荐",
                    null,
                    [$"按 {temperature}°C 推荐"]))
                .ToList();
        }
    }

    private sealed class FakeWeatherService : IWeatherService
    {
        private readonly WeatherInfo? _weather;

        public FakeWeatherService(WeatherInfo? weather)
        {
            _weather = weather;
        }

        public string? RequestedCity { get; private set; }

        public Task<WeatherInfo?> GetCurrentWeatherAsync(string city)
        {
            RequestedCity = city;
            return Task.FromResult(_weather);
        }
    }

    private sealed class FakeWeatherPreferencesService : IWeatherPreferencesService
    {
        private readonly string _city;

        public FakeWeatherPreferencesService(string city)
        {
            _city = city;
        }

        public Task<WeatherPreferences> GetAsync() => Task.FromResult(new WeatherPreferences { DefaultCity = _city });
        public Task SaveAsync(WeatherPreferences preferences) => Task.CompletedTask;
    }

    private sealed class FakeRecommendationPreferencesService : IRecommendationPreferencesService
    {
        private readonly RecommendationPreferences _preferences;

        public FakeRecommendationPreferencesService(RecommendationPreferences? preferences = null)
        {
            _preferences = preferences ?? new RecommendationPreferences();
        }

        public Task<RecommendationPreferences> GetAsync() => Task.FromResult(_preferences);
        public Task SaveAsync(RecommendationPreferences preferences) => Task.CompletedTask;
    }
}
