using System.IO;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Insights;
using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
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
        var weather = new FakeWeatherService(new WeatherInfo
        {
            City = "Hangzhou",
            Temperature = 18,
            Condition = "多云"
        });
        var viewModel = CreateViewModel([removed, kept], outfitService: outfitService, weatherService: weather);
        await viewModel.LoadOutfitsAsync();
        weather.ResetRequestCount();

        await viewModel.DeleteOutfitWithFeedbackAsync(removed);

        Assert.Equal(removed.Id, outfitService.DeletedOutfitId);
        Assert.Single(viewModel.Outfits);
        Assert.Equal("保留搭配", viewModel.Outfits[0].Name);
        Assert.Equal(0, weather.RequestCount);
    }

    [Fact]
    public async Task ToggleFavoriteWithFeedbackAsync_TogglesFavoriteAndRefreshesList()
    {
        var outfit = CreateOutfit("常穿搭配", OutfitScene.Work, Season.Autumn);
        var outfitService = new FakeOutfitService([outfit]);
        var weather = new FakeWeatherService(new WeatherInfo
        {
            City = "Hangzhou",
            Temperature = 18,
            Condition = "多云"
        });
        var viewModel = CreateViewModel([outfit], outfitService: outfitService, weatherService: weather);
        await viewModel.LoadOutfitsAsync();
        weather.ResetRequestCount();

        var result = await viewModel.ToggleFavoriteWithFeedbackAsync(outfit);

        Assert.True(result);
        Assert.Equal(outfit.Id, outfitService.ToggledFavoriteOutfitId);
        Assert.True(viewModel.Outfits[0].Favorites.Count > 0);
        Assert.Equal(0, weather.RequestCount);
    }

    [Fact]
    public async Task RecordOutfitWornWithFeedbackAsync_UpdatesStateWithoutRefreshingWeather()
    {
        var outfit = CreateOutfit("今日通勤", OutfitScene.Work, Season.Spring, withClothes: true);
        var outfitService = new FakeOutfitService([outfit]);
        var weather = new FakeWeatherService(new WeatherInfo
        {
            City = "Hangzhou",
            Temperature = 18,
            Condition = "多云"
        });
        var viewModel = CreateViewModel([outfit], outfitService: outfitService, weatherService: weather);
        await viewModel.LoadOutfitsAsync();
        weather.ResetRequestCount();

        await viewModel.RecordOutfitWornWithFeedbackAsync(outfit, outfit.Name);

        Assert.Equal(outfit.Id, outfitService.RecordedOutfitId);
        Assert.True(viewModel.HasTodayWornRecords);
        Assert.Equal(0, weather.RequestCount);
    }

    [Fact]
    public async Task RefreshAfterOutfitSavedWithFeedbackAsync_UpsertsSavedOutfitWithoutRefreshingWeather()
    {
        var first = CreateOutfit("原有搭配", OutfitScene.Work, Season.Autumn);
        var outfitService = new FakeOutfitService([first]);
        var weather = new FakeWeatherService(new WeatherInfo
        {
            City = "Hangzhou",
            Temperature = 18,
            Condition = "多云"
        });
        var viewModel = CreateViewModel([first], outfitService: outfitService, weatherService: weather);
        await viewModel.LoadOutfitsAsync();

        var added = CreateOutfit("新搭配", OutfitScene.Casual, Season.Spring);
        outfitService.AddStoredOutfit(added);
        weather.ResetRequestCount();

        await viewModel.RefreshAfterOutfitSavedWithFeedbackAsync(added.Id, "已保存搭配", "新的搭配已经出现在列表里。");

        Assert.Equal(2, viewModel.OutfitCount);
        Assert.Contains(viewModel.Outfits, outfit => outfit.Name == "新搭配");
        Assert.Equal(0, weather.RequestCount);
    }

    [Fact]
    public async Task EnsureCalendarLoadedAsync_LoadsCalendarAndNotifiesBindings()
    {
        var outfit = CreateOutfit("今日通勤", OutfitScene.Work, Season.Spring);
        var outfitService = new FakeOutfitService([outfit]);
        await outfitService.RecordWornDateAsync(outfit.Id, DateTime.Today);
        var viewModel = CreateViewModel([outfit], outfitService: outfitService);
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        await viewModel.EnsureCalendarLoadedAsync();

        Assert.Equal(42, viewModel.CalendarDays.Count);
        Assert.Contains(viewModel.CalendarDays, day => day.Date.Date == DateTime.Today && day.HasRecords);
        Assert.Contains(nameof(OutfitsViewModel.CalendarDays), changedProperties);
        Assert.True(viewModel.HasAnyWornRecords);
        Assert.False(viewModel.HasNoWornRecords);
    }

    [Fact]
    public async Task DisplayedOutfits_ReusesStableWindowAcrossLoadMoreAndFilters()
    {
        var outfits = Enumerable.Range(1, 25)
            .Select(index => CreateOutfit($"搭配 {index}", OutfitScene.Casual, Season.Spring))
            .ToArray();
        var viewModel = CreateViewModel(outfits);

        await viewModel.LoadOutfitsAsync();

        Assert.Equal(20, viewModel.DisplayedOutfits.Count);

        viewModel.LoadMoreOutfitsCommand.Execute(null);
        Assert.Equal(25, viewModel.DisplayedOutfits.Count);

        viewModel.SearchText = "搭配 1";
        Assert.All(viewModel.DisplayedOutfits, outfit => Assert.Contains("搭配 1", outfit.Name));

        viewModel.ClearFiltersCommand.Execute(null);
        Assert.Equal(20, viewModel.DisplayedOutfits.Count);
    }

    [Fact]
    public async Task SelectOutfit_SelectsMatchingStoredEntity()
    {
        var first = CreateOutfit("第一套", OutfitScene.Work, Season.Spring);
        var second = CreateOutfit("第二套", OutfitScene.Date, Season.Autumn);
        var viewModel = CreateViewModel([first, second]);
        await viewModel.LoadOutfitsAsync();

        viewModel.SelectOutfit(second);

        Assert.NotNull(viewModel.SelectedOutfit);
        Assert.Equal(second.Id, viewModel.SelectedOutfitId);
        Assert.Equal("第二套", viewModel.SelectedOutfit!.Name);
        Assert.True(viewModel.HasSelectedOutfit);
    }

    [Fact]
    public async Task LoadOutfitsAsync_LoadsDefaultCardDisplayMode()
    {
        using var displayPreferences = CreateDisplayPreferencesService(OutfitCardDisplayMode.EffectImageFirst);
        var viewModel = CreateViewModel([CreateOutfit("第一套", OutfitScene.Work, Season.Spring)], outfitDisplayPreferencesService: displayPreferences.Service);

        await viewModel.LoadOutfitsAsync();

        Assert.Equal(OutfitCardDisplayMode.EffectImageFirst, viewModel.CardDisplayMode);
        Assert.True(viewModel.IsEffectImageFirstDisplayMode);
    }

    [Fact]
    public async Task SetCardDisplayModeAsync_PersistsSelection()
    {
        using var displayPreferences = CreateDisplayPreferencesService(OutfitCardDisplayMode.OutfitFirst);
        var viewModel = CreateViewModel([CreateOutfit("第一套", OutfitScene.Work, Season.Spring)], outfitDisplayPreferencesService: displayPreferences.Service);

        await viewModel.SetCardDisplayModeAsync(OutfitCardDisplayMode.EffectImageFirst);

        var saved = await displayPreferences.Service.GetAsync();
        Assert.Equal(OutfitCardDisplayMode.EffectImageFirst, saved.DefaultCardDisplayMode);
        Assert.Equal(OutfitCardDisplayMode.EffectImageFirst, viewModel.CardDisplayMode);
    }

    [Fact]
    public async Task PreferenceChanged_UpdatesExistingViewModelDisplayMode()
    {
        using var displayPreferences = CreateDisplayPreferencesService(OutfitCardDisplayMode.OutfitFirst);
        var viewModel = CreateViewModel([CreateOutfit("第一套", OutfitScene.Work, Season.Spring)], outfitDisplayPreferencesService: displayPreferences.Service);
        await viewModel.LoadOutfitsAsync();

        await displayPreferences.Service.SaveAsync(new OutfitDisplayPreferences
        {
            DefaultCardDisplayMode = OutfitCardDisplayMode.EffectImageFirst
        });

        Assert.Equal(OutfitCardDisplayMode.EffectImageFirst, viewModel.CardDisplayMode);
    }

    [Fact]
    public async Task EffectImageOnlyFilter_ShowsOnlyOutfitsWithSucceededImages()
    {
        var withImage = CreateOutfit("有图搭配", OutfitScene.Work, Season.Spring);
        withImage.GeneratedImages.Add(new OutfitGeneratedImage
        {
            Id = Guid.NewGuid(),
            Status = "Succeeded",
            ResultImagePath = "render.png",
            CreatedAt = DateTime.Now
        });
        var withoutImage = CreateOutfit("没图搭配", OutfitScene.Work, Season.Spring);
        var viewModel = CreateViewModel([withImage, withoutImage]);

        await viewModel.LoadOutfitsAsync();
        viewModel.EffectImageOnly = true;

        var result = Assert.Single(viewModel.DisplayedOutfits);
        Assert.Equal("有图搭配", result.Name);
        Assert.Contains("仅有效果图", viewModel.FilterSummary);
    }

    [Fact]
    public async Task ClearFiltersCommand_AlsoResetsEffectImageOnly()
    {
        var withImage = CreateOutfit("有图搭配", OutfitScene.Work, Season.Spring);
        withImage.GeneratedImages.Add(new OutfitGeneratedImage
        {
            Id = Guid.NewGuid(),
            Status = "Succeeded",
            ResultImagePath = "render.png",
            CreatedAt = DateTime.Now
        });
        var withoutImage = CreateOutfit("没图搭配", OutfitScene.Work, Season.Spring);
        var viewModel = CreateViewModel([withImage, withoutImage]);

        await viewModel.LoadOutfitsAsync();
        viewModel.EffectImageOnly = true;
        viewModel.ClearFiltersCommand.Execute(null);

        Assert.False(viewModel.EffectImageOnly);
        Assert.Equal(2, viewModel.DisplayedOutfits.Count);
    }

    [Fact]
    public async Task DeleteOutfitAsync_WhenSelected_RemovesSelectionOrFallsBack()
    {
        var first = CreateOutfit("第一套", OutfitScene.Work, Season.Spring);
        var second = CreateOutfit("第二套", OutfitScene.Date, Season.Autumn);
        var outfitService = new FakeOutfitService([first, second]);
        var viewModel = CreateViewModel([first, second], outfitService: outfitService);
        await viewModel.LoadOutfitsAsync();
        viewModel.SelectOutfit(second);

        await viewModel.DeleteOutfitAsync(second);

        Assert.NotNull(viewModel.SelectedOutfit);
        Assert.Equal(first.Id, viewModel.SelectedOutfitId);
        Assert.Equal("第一套", viewModel.SelectedOutfit!.Name);
    }

    private static OutfitsViewModel CreateViewModel(
        IReadOnlyList<Outfit> outfits,
        FakeOutfitService? outfitService = null,
        IWeatherService? weatherService = null,
        FakeRecommendationPreferencesService? recommendationPreferencesService = null,
        FakeRecommendationService? recommendationService = null,
        OutfitDisplayPreferencesService? outfitDisplayPreferencesService = null)
    {
        var resolvedOutfitService = outfitService ?? new FakeOutfitService(outfits);
        var resolvedRecommendationService = recommendationService ?? new FakeRecommendationService(outfits);
        var getTodayRecommendations = new GetTodayRecommendations(
            resolvedRecommendationService,
            new GetRecommendationReadinessSummary(resolvedOutfitService));
        var getWardrobeInsights = new GetWardrobeInsights(resolvedOutfitService);
        var getAnnualOutfitReport = new GetAnnualOutfitReport(resolvedOutfitService);
        return new OutfitsViewModel(
            resolvedOutfitService,
            resolvedRecommendationService,
            weatherService ?? new FakeWeatherService(null),
            new FakeWeatherPreferencesService("Shanghai"),
            recommendationPreferencesService ?? new FakeRecommendationPreferencesService(),
            outfitDisplayPreferencesService ?? CreateDisplayPreferencesService(OutfitCardDisplayMode.OutfitFirst).Service,
            getTodayRecommendations,
            getWardrobeInsights,
            getAnnualOutfitReport);
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

        public Task<WornRecordImageHealthDto> AnalyzeWornRecordImageHealthAsync()
        {
            return Task.FromResult(new WornRecordImageHealthDto(_records.Count, 0, 0, 0));
        }

        public Task RepairWornRecordSnapshotImageAsync(Guid recordId, Guid clothingId, string imagePath)
        {
            throw new NotImplementedException();
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

        public Task<IReadOnlyList<OutfitGeneratedImage>> GetGeneratedImagesAsync(Guid outfitId)
        {
            var outfit = _outfits.FirstOrDefault(item => item.Id == outfitId);
            return Task.FromResult<IReadOnlyList<OutfitGeneratedImage>>(outfit?.GeneratedImages.ToList() ?? []);
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
        public Task<RecommendationDebugDto?> GetRecommendationDebugAsync(int temperature, OutfitScene? scene = null) => Task.FromResult<RecommendationDebugDto?>(null);
        public Task<RecommendationDebugDto?> GetRecommendationDebugForOutfitAsync(Guid outfitId, int temperature, OutfitScene? scene = null) => Task.FromResult<RecommendationDebugDto?>(null);

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
        public int RequestCount { get; private set; }

        public Task<WeatherInfo?> GetCurrentWeatherAsync(string city)
        {
            RequestedCity = city;
            RequestCount++;
            return Task.FromResult(_weather);
        }

        public int GetFallbackTemperature(DateTimeOffset? date = null) => 22;

        public void ResetRequestCount()
        {
            RequestCount = 0;
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

    private static TemporaryDisplayPreferencesService CreateDisplayPreferencesService(OutfitCardDisplayMode mode)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-outfit-display.json");
        var service = new OutfitDisplayPreferencesService(filePath);
        service.SaveAsync(new OutfitDisplayPreferences
        {
            DefaultCardDisplayMode = mode
        }).GetAwaiter().GetResult();
        return new TemporaryDisplayPreferencesService(filePath, service);
    }

    private sealed class TemporaryDisplayPreferencesService : IDisposable
    {
        public TemporaryDisplayPreferencesService(string filePath, OutfitDisplayPreferencesService service)
        {
            FilePath = filePath;
            Service = service;
        }

        public string FilePath { get; }
        public OutfitDisplayPreferencesService Service { get; }

        public void Dispose()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
    }
}
