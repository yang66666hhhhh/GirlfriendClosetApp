using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Insights;
using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Logic.States;
using ClosetApp.UI.Services;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class OutfitsViewModel : ViewModelBase
{
    private static readonly string[] StatePropertyNames =
    [
        nameof(Outfits),
        nameof(DisplayedOutfits),
        nameof(HasMoreOutfits),
        nameof(RecentWornRecords),
        nameof(SelectedRecentWornRecord),
        nameof(CalendarDays),
        nameof(IsLoading),
        nameof(IsEmpty),
        nameof(IsFilteredEmpty),
        nameof(OutfitCount),
        nameof(TotalCount),
        nameof(OutfitCountText),
        nameof(TotalCountText),
        nameof(HasActiveFilters),
        nameof(FilterSummary),
        nameof(FilterResultText),
        nameof(CollectionSectionTitle),
        nameof(CollectionSectionBody),
        nameof(FavoriteOnly),
        nameof(SelectedScene),
        nameof(SelectedSeason),
        nameof(SearchText),
        nameof(HistoryQuickText),
        nameof(HistorySummaryText),
        nameof(IsHistoryExpanded),
        nameof(HistoryToggleText),
        nameof(CalendarMonthText),
        nameof(CalendarSummaryText),
        nameof(HasAnyWornRecords),
        nameof(HasNoWornRecords),
        nameof(TodayWornCount),
        nameof(HasTodayWornRecords),
        nameof(TodayWornStatusText)
    ];

    private readonly IOutfitService _outfitService;
    private readonly OutfitsTabState _state = new();
    private int _displayedOutfitCount = 20;
    private const int PageSize = 20;

    private string _searchText = string.Empty;

    public OutfitsViewModel(
        IOutfitService outfitService,
        IOutfitRecommendationService outfitRecommendationService,
        IWeatherService weatherService,
        IWeatherPreferencesService weatherPreferencesService,
        IRecommendationPreferencesService recommendationPreferencesService,
        GetTodayRecommendations getTodayRecommendations,
        GetWardrobeInsights getWardrobeInsights,
        GetAnnualOutfitReport getAnnualOutfitReport)
    {
        _outfitService = outfitService;
        _outfitRecommendationService = outfitRecommendationService;
        _weatherService = weatherService;
        _weatherPreferencesService = weatherPreferencesService;
        _recommendationPreferencesService = recommendationPreferencesService;
        _getTodayRecommendations = getTodayRecommendations;
        _getWardrobeInsights = getWardrobeInsights;
        _getAnnualOutfitReport = getAnnualOutfitReport;
    }

    public IReadOnlyList<Outfit> Outfits => _state.Outfits;
    public IReadOnlyList<Outfit> DisplayedOutfits => _state.Outfits.Take(_displayedOutfitCount).ToList();
    public bool HasMoreOutfits => _state.Outfits.Count > _displayedOutfitCount;
    public IReadOnlyList<OutfitSceneFilterOption> SceneFilterOptions { get; } =
    [
        new("全部场景", null),
        new("通勤", OutfitScene.Work),
        new("约会", OutfitScene.Date),
        new("出游", OutfitScene.Travel),
        new("派对", OutfitScene.Party),
        new("休闲", OutfitScene.Casual)
    ];
    public IReadOnlyList<SeasonFilterOption> SeasonFilterOptions { get; } =
    [
        new("全部季节", null),
        new("春季", Season.Spring),
        new("夏季", Season.Summer),
        new("秋季", Season.Autumn),
        new("冬季", Season.Winter),
        new("四季", Season.AllSeason)
    ];
    public bool IsLoading => _state.IsLoading;
    public bool IsEmpty => _state.IsEmpty;
    public bool IsFilteredEmpty => _state.IsFilteredEmpty;
    public int OutfitCount => _state.OutfitCount;
    public int TotalCount => _state.TotalCount;
    public string OutfitCountText => $"{OutfitCount} 套搭配";
    public string TotalCountText => $"{TotalCount} 套搭配";
    public bool HasActiveFilters => _state.HasActiveFilters;
    public string FilterSummary => _state.FilterSummary;
    public string FilterResultText => HasActiveFilters
        ? $"{FilterSummary} · {OutfitCount} 套结果"
        : $"{FilterSummary} · {OutfitCount} 套";
    public string CollectionSectionTitle => HasActiveFilters ? "当前结果" : "全部搭配";
    public string CollectionSectionBody => HasActiveFilters
        ? $"按 {FilterSummary} 缩小到了 {OutfitCount} 套，继续改条件会更快。"
        : "按场景、季节、名称和收藏状态收窄结果，会更快找到今天那套。";
    public bool FavoriteOnly
    {
        get => _state.FavoriteOnly;
        set
        {
            if (_state.FavoriteOnly == value)
                return;

            _state.SetFavoriteOnly(value);
            NotifyStateChanged();
        }
    }

    public OutfitScene? SelectedScene
    {
        get => _state.SelectedScene;
        set => SetSelectedScene(value);
    }

    public Season? SelectedSeason
    {
        get => _state.SelectedSeason;
        set => SetSelectedSeason(value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
                return;

            _state.SetSearchText(value);
            NotifyStateChanged();
        }
    }

    public IReadOnlyList<OutfitSortBy> SortOptions { get; } = Enum.GetValues<OutfitSortBy>();

    public OutfitSortBy SortBy
    {
        get => _state.SortBy;
        set
        {
            if (_state.SortBy == value)
                return;

            _state.SetSortBy(value);
            NotifyStateChanged();
        }
    }

    public string GetSortLabel(OutfitSortBy sort) => OutfitPresentationText.GetSortLabel(sort);

    public async Task LoadOutfitsAsync()
    {
        _state.BeginLoad();
        NotifyStateChanged();

        try
        {
            var outfits = await _outfitService.GetAllOutfitsAsync();
            _state.SetOutfits(outfits);
            InvalidateInsightsCache();
            await RefreshDerivedStateAsync();
            await RefreshCalendarIfLoadedAsync();
            await RefreshWeatherRecommendationsAsync();
            Log.Debug("Loaded outfits. Count={OutfitCount}", OutfitCount);
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public Task RefreshAsync() => LoadOutfitsAsync();

    public void SetSelectedScene(OutfitScene? scene)
    {
        if (_state.SelectedScene == scene)
            return;

        _state.SetSelectedScene(scene);
        NotifyStateChanged();
    }

    public void SetSelectedSeason(Season? season)
    {
        if (_state.SelectedSeason == season)
            return;

        _state.SetSelectedSeason(season);
        NotifyStateChanged();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        SearchText = string.Empty;
        SetSelectedScene(null);
        SetSelectedSeason(null);
        FavoriteOnly = false;
        _displayedOutfitCount = PageSize;
        NotifyStateChanged();
    }

    [RelayCommand]
    public void LoadMoreOutfits()
    {
        _displayedOutfitCount += PageSize;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        NotifyPropertiesChanged(StatePropertyNames);
        NotifyWeatherStateChanged();
    }

    private void NotifyWeatherStateChanged()
    {
        NotifyPropertiesChanged(WeatherPropertyNames);
    }

}

public sealed record OutfitSceneFilterOption(string Label, OutfitScene? Value);

public sealed record SeasonFilterOption(string Label, Season? Value);
