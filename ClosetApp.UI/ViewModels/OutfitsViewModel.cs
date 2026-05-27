using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Services;
using ClosetApp.UI.States;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class OutfitsViewModel : ViewModelBase
{
    private static readonly string[] StatePropertyNames =
    [
        nameof(Outfits),
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
        nameof(TodayWornCount),
        nameof(HasTodayWornRecords),
        nameof(TodayWornStatusText)
    ];

    private static readonly string[] WeatherPropertyNames =
    [
        nameof(WeatherCity),
        nameof(WeatherTemperature),
        nameof(WeatherCondition),
        nameof(IsWeatherLoading),
        nameof(CanRefreshWeatherRecommendations),
        nameof(RefreshWeatherButtonText),
        nameof(WeatherStatusText),
        nameof(WeatherRecommendations),
        nameof(RecommendationReadiness),
        nameof(HasRecommendationReadiness),
        nameof(HasRecommendationGap),
        nameof(RecommendationReadinessTitle),
        nameof(RecommendationReadinessDetail),
        nameof(RecommendationReadinessBadgeText),
        nameof(RecommendationReadinessCountText),
        nameof(RecommendationMissingSeasonText),
        nameof(HasWeatherRecommendations),
        nameof(HasPrimaryWeatherRecommendation),
        nameof(HasSecondaryWeatherRecommendations),
        nameof(WeatherHeadlineText),
        nameof(WeatherCityCompactText),
        nameof(WeatherCompactSummaryText),
        nameof(WeatherRecommendationCountText),
        nameof(PrimaryWeatherRecommendation),
        nameof(SecondaryWeatherRecommendations),
        nameof(WeatherRecommendationHintText),
        nameof(TodayHeroRecommendationNameText),
        nameof(TodayHeroRecommendationSupportText),
        nameof(TodayHeroPrimaryActionText)
    ];

    private readonly IOutfitService _outfitService;
    private readonly IOutfitRecommendationService _recommendationService;
    private readonly IWeatherService _weatherService;
    private readonly IWeatherPreferencesService _weatherPreferencesService;
    private readonly GetRecommendationReadinessSummary _getRecommendationReadinessSummary;
    private readonly OutfitsTabState _state = new();

    [ObservableProperty]
    private string _weatherCity = "Shanghai";

    [ObservableProperty]
    private int _weatherTemperature = 22;

    [ObservableProperty]
    private string _weatherCondition = "晴";

    [ObservableProperty]
    private bool _isWeatherLoading;

    [ObservableProperty]
    private string _weatherStatusText = "正在根据当前天气整理推荐搭配。";

    [ObservableProperty]
    private IReadOnlyList<RecommendedOutfitDto> _weatherRecommendations = [];

    [ObservableProperty]
    private RecommendationReadinessSummaryDto? _recommendationReadiness;

    private string _searchText = string.Empty;

    public OutfitsViewModel(
        IOutfitService outfitService,
        IOutfitRecommendationService recommendationService,
        IWeatherService weatherService,
        IWeatherPreferencesService weatherPreferencesService,
        GetRecommendationReadinessSummary getRecommendationReadinessSummary)
    {
        _outfitService = outfitService;
        _recommendationService = recommendationService;
        _weatherService = weatherService;
        _weatherPreferencesService = weatherPreferencesService;
        _getRecommendationReadinessSummary = getRecommendationReadinessSummary;
    }

    public IReadOnlyList<Outfit> Outfits => _state.Outfits;
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
    public IReadOnlyList<RecentWornListItem> RecentWornRecords => _state.RecentWornRecords;
    public RecentWornListItem? SelectedRecentWornRecord => _state.SelectedRecentWornRecord;
    public IReadOnlyList<CalendarDayItem> CalendarDays => _state.CalendarDays;
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

    public string GetSortLabel(OutfitSortBy sort) => sort switch
    {
        OutfitSortBy.Newest => "最新创建",
        OutfitSortBy.Oldest => "最早创建",
        OutfitSortBy.Name => "名称",
        OutfitSortBy.Rating => "评分",
        OutfitSortBy.WearCount => "穿着次数",
        OutfitSortBy.LastWorn => "最近穿着",
        _ => sort.ToString()
    };

    public string HistoryQuickText => _state.HistoryQuickText;
    public string HistorySummaryText => _state.HistorySummaryText;
    public bool IsHistoryExpanded => _state.IsHistoryExpanded;
    public string HistoryToggleText => _state.HistoryToggleText;
    public string CalendarMonthText => _state.CalendarMonthText;
    public string CalendarSummaryText => _state.CalendarSummaryText;
    public string WeatherHeadlineText => $"{WeatherCity} · {WeatherTemperature}°C · {WeatherCondition}";
    public string WeatherCityCompactText => BuildCompactWeatherCity(WeatherCity);
    public string WeatherCompactSummaryText => $"{WeatherTemperature}°C · {WeatherCondition}";
    public int TodayWornCount => RecentWornRecords.Count(record => record.WornDate.Date == DateTime.Today);
    public bool HasTodayWornRecords => TodayWornCount > 0;
    public string TodayWornStatusText => HasTodayWornRecords ? $"今天已记 {TodayWornCount} 套" : "今天还没记录";
    public bool HasWeatherRecommendations => WeatherRecommendations.Count > 0;
    public bool HasPrimaryWeatherRecommendation => PrimaryWeatherRecommendation != null;
    public bool HasSecondaryWeatherRecommendations => SecondaryWeatherRecommendations.Count > 0;
    public string WeatherRecommendationCountText => HasWeatherRecommendations ? $"{WeatherRecommendations.Count} 套" : "暂无";
    public RecommendedOutfitDto? PrimaryWeatherRecommendation => WeatherRecommendations.FirstOrDefault();
    public IReadOnlyList<RecommendedOutfitDto> SecondaryWeatherRecommendations => WeatherRecommendations.Skip(1).Take(2).ToList();
    public bool CanRefreshWeatherRecommendations => !IsWeatherLoading;
    public string RefreshWeatherButtonText => IsWeatherLoading ? "刷新中..." : "刷新天气推荐";
    public bool HasRecommendationReadiness => RecommendationReadiness != null;
    public bool HasRecommendationGap => RecommendationReadiness?.HasGap ?? false;
    public string RecommendationReadinessTitle => RecommendationReadiness?.Title ?? "推荐准备度";
    public string RecommendationReadinessDetail => RecommendationReadiness?.Detail ?? "刷新天气后会整理当前搭配是否够用。";
    public string RecommendationReadinessBadgeText => HasRecommendationGap ? "还差一点" : "已经就绪";
    public string RecommendationReadinessCountText => RecommendationReadiness == null
        ? "等待刷新"
        : RecommendationReadiness.MatchingSeasonCount > 0
            ? $"{RecommendationReadiness.MatchingSeasonCount}/{RecommendationReadiness.ReadyOutfitCount} 套对季"
            : $"{RecommendationReadiness.ReadyOutfitCount} 套已整理";
    public string RecommendationMissingSeasonText => RecommendationReadiness?.MissingSeason is { } season
        ? $"建议补 {GetSeasonLabel(season)} 搭配"
        : HasRecommendationGap
            ? "先把常穿搭配补完整，推荐会更稳。"
            : "当前温度下已经有可轮换的搭配。";
    public string WeatherRecommendationHintText => WeatherRecommendations.Count == 0
        ? RecommendationReadinessDetail
        : $"{PrimaryWeatherRecommendation!.PrimaryReason}";
    public string TodayHeroRecommendationNameText => HasPrimaryWeatherRecommendation
        ? PrimaryWeatherRecommendation!.Name
        : "今天还没有合适推荐";
    public string TodayHeroRecommendationSupportText => HasPrimaryWeatherRecommendation
        ? BuildTodayHeroSupportText(PrimaryWeatherRecommendation!)
        : HasTodayWornRecords
            ? $"{TodayWornStatusText}，{RecommendationReadinessDetail}"
            : RecommendationReadinessDetail;
    public string TodayHeroPrimaryActionText => HasPrimaryWeatherRecommendation
        ? PrimaryWeatherRecommendation!.IsWornToday
            ? "今天又穿它"
            : HasTodayWornRecords
                ? "再记这套"
                : "今天穿它"
        : "去新建一套";

    public async Task LoadOutfitsAsync()
    {
        _state.BeginLoad();
        NotifyStateChanged();

        try
        {
            var outfits = await _outfitService.GetAllOutfitsAsync();
            _state.SetOutfits(outfits);
            await RefreshDerivedStateAsync();
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
        NotifyStateChanged();
    }

    public async Task DeleteOutfitAsync(Outfit outfit)
    {
        await _outfitService.DeleteOutfitAsync(outfit.Id);
        await LoadOutfitsAsync();
    }

    public async Task RecordWornDateAsync(Outfit outfit, DateTime date)
    {
        await _outfitService.RecordWornDateAsync(outfit.Id, date);
        await LoadOutfitsAsync();
    }

    public Task RecordOutfitWornTodayAsync(Outfit outfit) => RecordWornDateAsync(outfit, DateTime.Now);

    public async Task RecordOutfitWornWithFeedbackAsync(
        Outfit outfit,
        string displayName,
        string detail = "今天的穿着记录已经更新。")
    {
        try
        {
            await RecordOutfitWornTodayAsync(outfit);
            ToastService.Instance.ShowSuccess($"已记录穿过「{displayName}」", detail);
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForOutfitRecord(ex, displayName);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }

    [RelayCommand]
    public Task RecordRecommendedOutfitWornAsync(RecommendedOutfitDto? recommendation)
    {
        return recommendation == null
            ? Task.CompletedTask
            : RecordOutfitWornWithFeedbackAsync(
                recommendation.Outfit,
                recommendation.Name,
                "今日推荐已经同步到穿着记录。");
    }

    public Task RefreshAfterOutfitSavedAsync() => LoadOutfitsAsync();

    public async Task<bool> ToggleFavoriteAsync(Outfit outfit)
    {
        var isFav = await _outfitService.ToggleFavoriteAsync(outfit.Id);
        await LoadOutfitsAsync();
        return isFav;
    }

    public async Task RefreshWeatherRecommendationsAsync()
    {
        IsWeatherLoading = true;
        WeatherStatusText = "正在刷新当前天气和推荐搭配...";

        try
        {
            var preferences = await _weatherPreferencesService.GetAsync();
            WeatherCity = preferences.DefaultCity;

            var weather = await _weatherService.GetCurrentWeatherAsync(WeatherCity);
            if (weather != null)
            {
                WeatherCity = weather.City;
                WeatherTemperature = weather.Temperature;
                WeatherCondition = weather.Condition;
            }
            else
            {
                WeatherTemperature = ResolveFallbackTemperature(DateTime.Now);
                WeatherCondition = "天气暂缺";
                WeatherStatusText = $"暂时拿不到 {WeatherCity} 的天气，先按 {WeatherTemperature}°C 的季节体感继续推荐。";
            }

            WeatherRecommendations = (await _recommendationService.GetRecommendationsByRuleAsync(WeatherTemperature, null))
                .Take(3)
                .ToList();
            RecommendationReadiness = await _getRecommendationReadinessSummary.ExecuteAsync(WeatherTemperature);

            if (WeatherRecommendations.Count > 0 && weather != null)
                WeatherStatusText = "已按当前天气刷新推荐。";
            else if (WeatherRecommendations.Count > 0)
                WeatherStatusText = "天气暂时缺席，但我还是先按体感温度帮你挑了几套。";
            else if (weather != null)
                WeatherStatusText = "天气已刷新，但衣橱里还没有匹配出来的搭配。";
        }
        catch (Exception ex)
        {
            WeatherRecommendations = [];
            WeatherStatusText = $"刷新推荐失败：{ex.Message}";
            Log.Warning(ex, "Failed to refresh weather recommendations for outfits tab");
        }
        finally
        {
            IsWeatherLoading = false;
            NotifyWeatherStateChanged();
        }
    }

    [RelayCommand]
    public async Task RefreshWeatherRecommendationsWithFeedbackAsync()
    {
        try
        {
            await RefreshWeatherRecommendationsAsync();
            ToastService.Instance.ShowSuccess("已刷新天气推荐", "当前城市的天气和今日推荐已经更新。");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("刷新天气推荐失败", ex.Message);
        }
    }

    public void ToggleHistoryExpanded()
    {
        _state.ToggleHistoryExpanded();
        NotifyStateChanged();
    }

    public async Task MoveCalendarMonthAsync(int offsetMonths)
    {
        _state.MoveCalendarMonth(offsetMonths);
        await RefreshCalendarAsync();
        NotifyStateChanged();
    }

    public async Task FocusHistoryDateAsync(DateTime date)
    {
        var monthChanged = _state.SelectHistoryDate(date);
        if (monthChanged)
            await RefreshCalendarAsync();

        NotifyStateChanged();
    }

    public async Task FocusHistoryRecordAsync(Guid recordId, DateTime date)
    {
        var monthChanged = _state.SelectHistoryRecord(recordId, date);
        if (monthChanged)
            await RefreshCalendarAsync();

        NotifyStateChanged();
    }

    private async Task RefreshDerivedStateAsync()
    {
        var recentRecords = await _outfitService.GetRecentWornRecordsAsync(6);
        _state.SetRecentWornRecords(recentRecords);
        await RefreshCalendarAsync();
    }

    private async Task RefreshCalendarAsync()
    {
        var monthStart = _state.CalendarMonth;
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var monthRecords = await _outfitService.GetWornRecordsAsync(monthStart, monthEnd);
        _state.SetCalendarRecords(monthRecords);
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

    private static string BuildCompactWeatherCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return string.Empty;

        var parts = city
            .Split(" · ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .ToArray();

        return parts.Length == 0 ? city : string.Join(" · ", parts);
    }

    private static int ResolveFallbackTemperature(DateTime now)
    {
        return now.Month switch
        {
            12 or 1 or 2 => 8,
            3 or 4 or 11 => 16,
            5 or 10 => 22,
            _ => 28
        };
    }

    private static string GetSeasonLabel(Season season)
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

    private string BuildTodayHeroSupportText(RecommendedOutfitDto recommendation)
    {
        if (recommendation.IsWornToday)
            return "这套今天已经记过一次了；如果晚点还要出门，也可以继续穿它。";

        var summary = ResolveHeroSummaryText(recommendation);
        if (!HasTodayWornRecords)
            return summary;

        return $"{summary} 今天已经记过 {TodayWornCount} 套，下一套可以换个感觉。";
    }

    private static string ResolveHeroSummaryText(RecommendedOutfitDto recommendation)
    {
        var summary = recommendation.ReasonSummaryText?.Trim();
        if (!string.IsNullOrWhiteSpace(summary))
            return summary;

        var primary = recommendation.PrimaryReason?.Trim();
        return string.IsNullOrWhiteSpace(primary) ? "今天先穿这套。" : primary;
    }
}

public sealed record OutfitSceneFilterOption(string Label, OutfitScene? Value);

public sealed record SeasonFilterOption(string Label, Season? Value);
