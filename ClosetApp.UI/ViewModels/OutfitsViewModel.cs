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
    private readonly GetWardrobeInsights _getWardrobeInsights;
    private readonly GetAnnualOutfitReport _getAnnualOutfitReport;
    private readonly OutfitsTabState _state = new();
    private WardrobeInsightsDto? _cachedInsights;
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
        InvalidateRecommendationDebugCache();
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
