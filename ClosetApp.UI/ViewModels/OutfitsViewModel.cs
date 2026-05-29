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
    private readonly IOutfitRecommendationService _outfitRecommendationService;
    private readonly IWeatherService _weatherService;
    private readonly IWeatherPreferencesService _weatherPreferencesService;
    private readonly IRecommendationPreferencesService _recommendationPreferencesService;
    private readonly GetTodayRecommendations _getTodayRecommendations;
    private readonly GetWardrobeInsights _getWardrobeInsights;
    private readonly GetAnnualOutfitReport _getAnnualOutfitReport;
    private readonly OutfitsTabState _state = new();
    private WardrobeInsightsDto? _cachedInsights;
    private RecommendationDebugDto? _cachedBestDebug;
    private readonly Dictionary<Guid, RecommendationDebugDto> _cachedOutfitDebugs = new();
    private int _displayedOutfitCount = 20;
    private const int PageSize = 20;

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
    public bool HasAnyWornRecords => RecentWornRecords.Count > 0;
    public bool HasNoWornRecords => !HasAnyWornRecords;
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

    public async Task DeleteOutfitAsync(Outfit outfit)
    {
        await _outfitService.DeleteOutfitAsync(outfit.Id);
        await LoadOutfitsAsync();
    }

    public async Task DeleteOutfitWithFeedbackAsync(Outfit outfit)
    {
        try
        {
            await DeleteOutfitAsync(outfit);
            ToastService.Instance.ShowSuccess($"已删除「{outfit.Name}」", "这套搭配已经从列表移除。");
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForOutfitDelete(ex, outfit.Name);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
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

    [RelayCommand]
    public async Task ShowRecommendationDebugAsync()
    {
        try
        {
            if (_cachedBestDebug == null)
            {
                var temperature = WeatherTemperature;
                var scene = await GetDefaultSceneAsync();
                _cachedBestDebug = await _outfitRecommendationService.GetRecommendationDebugAsync(temperature, scene);
            }

            if (_cachedBestDebug == null)
            {
                ToastService.Instance.ShowInfo("暂无推荐详情", "当前天气条件下还没有匹配的搭配数据，试试先建几套不同季节的搭配。");
                return;
            }

            ModalService.Instance.Show(new ClosetApp.UI.Components.Shared.Modal.RecommendationDebugDialog(_cachedBestDebug));
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("推荐详情加载失败", $"无法获取当前温度 {WeatherTemperature}°C 的推荐数据：{ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ShowRecommendationDebugForOutfitAsync(RecommendedOutfitDto? recommendation)
    {
        if (recommendation == null) return;

        try
        {
            var outfitId = recommendation.Outfit.Id;
            if (!_cachedOutfitDebugs.TryGetValue(outfitId, out var debug))
            {
                var temperature = WeatherTemperature;
                var scene = await GetDefaultSceneAsync();
                debug = await _outfitRecommendationService.GetRecommendationDebugForOutfitAsync(outfitId, temperature, scene);
                if (debug != null)
                    _cachedOutfitDebugs[outfitId] = debug;
            }

            if (debug == null)
            {
                ToastService.Instance.ShowInfo("暂无该搭配的推荐数据", $"「{recommendation.Name}」还没有推荐评分记录，可能需要先完成一次天气推荐刷新。");
                return;
            }

            ModalService.Instance.Show(new ClosetApp.UI.Components.Shared.Modal.RecommendationDebugDialog(debug));
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError($"「{recommendation.Name}」的推荐详情加载失败", ex.Message);
        }
    }

    private async Task<OutfitScene?> GetDefaultSceneAsync()
    {
        try
        {
            var preferences = await _recommendationPreferencesService.GetAsync();
            return preferences.DefaultScene;
        }
        catch
        {
            return null;
        }
    }

    [RelayCommand]
    public async Task ShowWardrobeInsightsAsync()
    {
        try
        {
            _cachedInsights ??= await _getWardrobeInsights.ExecuteAsync();
            ModalService.Instance.Show(new ClosetApp.UI.Components.Shared.Modal.WardrobeInsightsDialog(_cachedInsights));
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("衣柜统计数据加载失败", $"无法生成当前衣柜的统计分析：{ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ShowAnnualReportAsync()
    {
        try
        {
            var year = DateTime.Now.Year;
            var report = await _getAnnualOutfitReport.ExecuteAsync(year);
            ModalService.Instance.Show(new ClosetApp.UI.Components.Shared.Modal.AnnualOutfitReportDialog(report));
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError($"{DateTime.Now.Year}年度报告加载失败", $"无法生成年度穿搭报告：{ex.Message}");
        }
    }

    private void InvalidateInsightsCache()
    {
        _cachedInsights = null;
        _cachedBestDebug = null;
        _cachedOutfitDebugs.Clear();
    }

    public Task RefreshAfterOutfitSavedAsync() => LoadOutfitsAsync();

    public async Task RefreshAfterOutfitSavedWithFeedbackAsync(string title, string detail)
    {
        try
        {
            await RefreshAfterOutfitSavedAsync();
            ToastService.Instance.ShowSuccess(title, detail);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("搭配列表刷新失败", $"保存成功但列表未能更新：{ex.Message}");
        }
    }

    public async Task<bool> ToggleFavoriteAsync(Outfit outfit)
    {
        var isFav = await _outfitService.ToggleFavoriteAsync(outfit.Id);
        await LoadOutfitsAsync();
        return isFav;
    }

    public async Task<bool?> ToggleFavoriteWithFeedbackAsync(Outfit outfit)
    {
        try
        {
            var isFav = await ToggleFavoriteAsync(outfit);
            var name = outfit.Name?.Trim();
            var displayName = !string.IsNullOrWhiteSpace(name) ? $"「{name}」" : "该搭配";
            ToastService.Instance.ShowSuccess(isFav ? $"已收藏{displayName}" : $"已取消收藏{displayName}");
            return isFav;
        }
        catch (Exception ex)
        {
            var name = outfit.Name?.Trim();
            var displayName = !string.IsNullOrWhiteSpace(name) ? $"「{name}」" : "该搭配";
            ToastService.Instance.ShowError($"收藏{displayName}失败", ex.Message);
            return null;
        }
    }

    public async Task RefreshWeatherRecommendationsAsync()
    {
        IsWeatherLoading = true;
        WeatherStatusText = "正在刷新当前天气和推荐搭配...";

        try
        {
            var weatherPreferences = await _weatherPreferencesService.GetAsync();
            var recommendationPreferences = await _recommendationPreferencesService.GetAsync();
            var city = weatherPreferences.DefaultCity;

            var weather = await _weatherService.GetCurrentWeatherAsync(city);
            int temperature;
            bool isWeatherFromApi;
            string condition;

            if (weather != null)
            {
                city = weather.City;
                temperature = weather.Temperature;
                condition = weather.Condition;
                isWeatherFromApi = true;
            }
            else
            {
                temperature = _weatherService.GetFallbackTemperature();
                condition = "天气暂缺";
                isWeatherFromApi = false;
            }

            var request = new TodayRecommendationRequest(
                city,
                temperature,
                condition,
                isWeatherFromApi,
                recommendationPreferences.DefaultScene,
                recommendationPreferences.AvoidWornToday,
                recommendationPreferences.RotationStrategy);

            var result = await _getTodayRecommendations.ExecuteAsync(request);

            WeatherCity = result.City;
            WeatherTemperature = result.Temperature;
            WeatherCondition = result.Condition;
            WeatherRecommendations = result.Recommendations;
            RecommendationReadiness = result.Readiness;
            WeatherStatusText = result.StatusText;
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
            ToastService.Instance.ShowError("天气推荐刷新失败", $"无法获取「{WeatherCity}」的天气数据：{ex.Message}");
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
    }

    public async Task EnsureCalendarLoadedAsync()
    {
        if (_state.RecentWornRecords.Count == 0)
            await RefreshDerivedStateAsync();

        if (_state.CalendarDays.Count == 0)
            await RefreshCalendarAsync();

        NotifyStateChanged();
    }

    private async Task RefreshCalendarIfLoadedAsync()
    {
        if (_state.CalendarDays.Count > 0)
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
