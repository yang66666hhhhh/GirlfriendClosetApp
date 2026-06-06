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
        nameof(EffectImageOnly),
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
        nameof(TodayWornStatusText),
        nameof(SelectedOutfit),
        nameof(SelectedOutfitId),
        nameof(HasSelectedOutfit)
    ];

    private readonly IOutfitService _outfitService;
    private readonly GenerateOutfitEffectImage? _generateOutfitEffectImage;
    private readonly GetOutfitGeneratedImages? _getOutfitGeneratedImages;
    private readonly DeleteOutfitGeneratedImage? _deleteOutfitGeneratedImage;
    private readonly SetPrimaryOutfitGeneratedImage? _setPrimaryOutfitGeneratedImage;
    private readonly OutfitDisplayPreferencesService _outfitDisplayPreferencesService;
    private readonly OutfitsTabState _state = new();
    private IReadOnlyList<Outfit> _displayedOutfits = [];
    private int _displayedOutfitCount = 20;
    private const int PageSize = 20;
    private Outfit? _selectedOutfit;

    private string _searchText = string.Empty;
    private OutfitCardDisplayMode _cardDisplayMode = OutfitCardDisplayMode.OutfitFirst;
    private bool _hasLoadedCardDisplayMode;

    public OutfitsViewModel(
        IOutfitService outfitService,
        IOutfitRecommendationService outfitRecommendationService,
        IWeatherService weatherService,
        IWeatherPreferencesService weatherPreferencesService,
        IRecommendationPreferencesService recommendationPreferencesService,
        OutfitDisplayPreferencesService outfitDisplayPreferencesService,
        GetTodayRecommendations getTodayRecommendations,
        GetWardrobeInsights getWardrobeInsights,
        GetAnnualOutfitReport getAnnualOutfitReport,
        GenerateOutfitEffectImage? generateOutfitEffectImage = null,
        GetOutfitGeneratedImages? getOutfitGeneratedImages = null,
        DeleteOutfitGeneratedImage? deleteOutfitGeneratedImage = null,
        SetPrimaryOutfitGeneratedImage? setPrimaryOutfitGeneratedImage = null)
    {
        _outfitService = outfitService;
        _outfitRecommendationService = outfitRecommendationService;
        _weatherService = weatherService;
        _weatherPreferencesService = weatherPreferencesService;
        _recommendationPreferencesService = recommendationPreferencesService;
        _outfitDisplayPreferencesService = outfitDisplayPreferencesService;
        _getTodayRecommendations = getTodayRecommendations;
        _getWardrobeInsights = getWardrobeInsights;
        _getAnnualOutfitReport = getAnnualOutfitReport;
        _generateOutfitEffectImage = generateOutfitEffectImage;
        _getOutfitGeneratedImages = getOutfitGeneratedImages;
        _deleteOutfitGeneratedImage = deleteOutfitGeneratedImage;
        _setPrimaryOutfitGeneratedImage = setPrimaryOutfitGeneratedImage;
        _outfitDisplayPreferencesService.PreferenceChanged += OutfitDisplayPreferencesService_PreferenceChanged;
    }

    public IReadOnlyList<Outfit> Outfits => _state.Outfits;
    public IReadOnlyList<Outfit> DisplayedOutfits => _displayedOutfits;
    public bool HasMoreOutfits => _state.Outfits.Count > _displayedOutfitCount;
    public Outfit? SelectedOutfit
    {
        get => _selectedOutfit;
        private set
        {
            if (ReferenceEquals(_selectedOutfit, value))
                return;

            _selectedOutfit = value;
            OnPropertyChanged(nameof(SelectedOutfit));
            OnPropertyChanged(nameof(SelectedOutfitId));
            OnPropertyChanged(nameof(HasSelectedOutfit));
        }
    }

    public Guid? SelectedOutfitId => SelectedOutfit?.Id;
    public bool HasSelectedOutfit => SelectedOutfit != null;
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
    public OutfitCardDisplayMode CardDisplayMode
    {
        get => _cardDisplayMode;
        private set => SetProperty(ref _cardDisplayMode, value);
    }
    public bool IsOutfitFirstDisplayMode => CardDisplayMode == OutfitCardDisplayMode.OutfitFirst;
    public bool IsEffectImageFirstDisplayMode => CardDisplayMode == OutfitCardDisplayMode.EffectImageFirst;
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
        ? $"{FilterSummary} · {OutfitCount} 套"
        : "按筛选条件查看搭配。";
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

    public bool EffectImageOnly
    {
        get => _state.EffectImageOnly;
        set
        {
            if (_state.EffectImageOnly == value)
                return;

            _state.SetEffectImageOnly(value);
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

    public async Task<Outfit?> RefreshSingleOutfitAsync(Guid outfitId)
    {
        var outfit = await _outfitService.GetOutfitByIdAsync(outfitId);
        if (outfit == null)
        {
            _state.RemoveOutfit(outfitId);
            if (SelectedOutfit?.Id == outfitId)
            {
                SelectedOutfit = _state.Outfits.FirstOrDefault();
            }
        }
        else
        {
            _state.UpsertOutfit(outfit);
            if (SelectedOutfit?.Id == outfitId)
            {
                SelectedOutfit = outfit;
            }
        }

        InvalidateInsightsCache();
        await RefreshDerivedStateAsync();
        await RefreshCalendarIfLoadedAsync();
        await RefreshRecommendationsForCurrentWeatherAsync();
        NotifyStateChanged();
        return outfit;
    }

    public async Task LoadOutfitsAsync(bool refreshWeather = false)
    {
        await EnsureCardDisplayModeLoadedAsync();
        _state.BeginLoad();
        NotifyStateChanged();

        try
        {
            var outfits = await _outfitService.GetAllOutfitsAsync();
            _state.SetOutfits(outfits);
            SelectedOutfit = ResolveSelection(SelectedOutfit?.Id);
            InvalidateInsightsCache();
            await RefreshDerivedStateAsync();
            await RefreshCalendarIfLoadedAsync();

            Log.Debug("Loaded outfits. Count={OutfitCount}", OutfitCount);
        }
        finally
        {
            NotifyStateChanged();
        }

        if (refreshWeather || WeatherRecommendations.Count == 0)
        {
            _ = RefreshWeatherRecommendationsInBackgroundAsync();
        }
        else
        {
            _ = RefreshRecommendationsForCurrentWeatherInBackgroundAsync();
        }
    }

    public Task RefreshAsync() => LoadOutfitsAsync(refreshWeather: true);

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
        EffectImageOnly = false;
        _displayedOutfitCount = PageSize;
        NotifyStateChanged();
    }

    [RelayCommand]
    public void LoadMoreOutfits()
    {
        _displayedOutfitCount += PageSize;
        NotifyStateChanged();
    }

    public async Task SetCardDisplayModeAsync(OutfitCardDisplayMode mode)
    {
        if (CardDisplayMode == mode)
            return;

        await _outfitDisplayPreferencesService.SaveAsync(new OutfitDisplayPreferences
        {
            DefaultCardDisplayMode = mode
        });
    }

    private void NotifyStateChanged()
    {
        RefreshDisplayedOutfits();
        NotifyPropertiesChanged(StatePropertyNames);
        NotifyWeatherStateChanged();
    }

    private void NotifyWeatherStateChanged()
    {
        NotifyPropertiesChanged(WeatherPropertyNames);
    }

    private void RefreshDisplayedOutfits()
    {
        var visibleCount = Math.Min(_displayedOutfitCount, _state.Outfits.Count);
        _displayedOutfits = visibleCount <= 0
            ? []
            : _state.Outfits.Take(visibleCount).ToArray();
    }

    public void SelectOutfit(Outfit? outfit)
    {
        SelectedOutfit = outfit == null ? null : ResolveSelection(outfit.Id);
    }

    public void ClearSelectedOutfit()
    {
        SelectedOutfit = null;
    }

    private Outfit? ResolveSelection(Guid? outfitId)
    {
        if (outfitId == null)
            return null;

        return _state.Outfits.FirstOrDefault(outfit => outfit.Id == outfitId);
    }

    private async Task EnsureCardDisplayModeLoadedAsync()
    {
        if (_hasLoadedCardDisplayMode)
            return;

        var preferences = await _outfitDisplayPreferencesService.GetAsync();
        _hasLoadedCardDisplayMode = true;
        ApplyCardDisplayMode(preferences.DefaultCardDisplayMode);
    }

    private void ApplyCardDisplayMode(OutfitCardDisplayMode mode)
    {
        if (CardDisplayMode == mode)
            return;

        CardDisplayMode = mode;
        NotifyPropertiesChanged(nameof(CardDisplayMode), nameof(IsOutfitFirstDisplayMode), nameof(IsEffectImageFirstDisplayMode));
    }

    private void OutfitDisplayPreferencesService_PreferenceChanged(object? sender, OutfitDisplayPreferencesChangedEventArgs e)
    {
        _hasLoadedCardDisplayMode = true;
        ApplyCardDisplayMode(e.Preferences.DefaultCardDisplayMode);
    }

}

public sealed record OutfitSceneFilterOption(string Label, OutfitScene? Value);

public sealed record SeasonFilterOption(string Label, Season? Value);
