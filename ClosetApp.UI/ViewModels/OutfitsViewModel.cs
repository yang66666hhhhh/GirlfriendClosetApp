using CommunityToolkit.Mvvm.ComponentModel;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.States;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class OutfitsViewModel : ObservableObject
{
    private readonly IOutfitService _outfitService;
    private readonly IOutfitRecommendationService _recommendationService;
    private readonly IWeatherService _weatherService;
    private readonly IWeatherPreferencesService _weatherPreferencesService;
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

    public OutfitsViewModel(
        IOutfitService outfitService,
        IOutfitRecommendationService recommendationService,
        IWeatherService weatherService,
        IWeatherPreferencesService weatherPreferencesService)
    {
        _outfitService = outfitService;
        _recommendationService = recommendationService;
        _weatherService = weatherService;
        _weatherPreferencesService = weatherPreferencesService;
    }

    public IReadOnlyList<Outfit> Outfits => _state.Outfits;
    public IReadOnlyList<RecentWornListItem> RecentWornRecords => _state.RecentWornRecords;
    public IReadOnlyList<CalendarDayItem> CalendarDays => _state.CalendarDays;
    public bool IsLoading => _state.IsLoading;
    public bool IsEmpty => _state.IsEmpty;
    public int OutfitCount => _state.OutfitCount;
    public string OutfitCountText => $"{OutfitCount} 套搭配";

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
    public bool HasWeatherRecommendations => WeatherRecommendations.Count > 0;
    public string WeatherRecommendationCountText => HasWeatherRecommendations ? $"{WeatherRecommendations.Count} 套" : "暂无";
    public RecommendedOutfitDto? PrimaryWeatherRecommendation => WeatherRecommendations.FirstOrDefault();
    public string WeatherRecommendationHintText => WeatherRecommendations.Count == 0
        ? BuildNoRecommendationHint()
        : $"{PrimaryWeatherRecommendation!.PrimaryReason}";

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
        OnPropertyChanged(nameof(Outfits));
        OnPropertyChanged(nameof(RecentWornRecords));
        OnPropertyChanged(nameof(CalendarDays));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(OutfitCount));
        OnPropertyChanged(nameof(OutfitCountText));
        OnPropertyChanged(nameof(HistoryQuickText));
        OnPropertyChanged(nameof(HistorySummaryText));
        OnPropertyChanged(nameof(IsHistoryExpanded));
        OnPropertyChanged(nameof(HistoryToggleText));
        OnPropertyChanged(nameof(CalendarMonthText));
        OnPropertyChanged(nameof(CalendarSummaryText));
        NotifyWeatherStateChanged();
    }

    private void NotifyWeatherStateChanged()
    {
        OnPropertyChanged(nameof(WeatherCity));
        OnPropertyChanged(nameof(WeatherTemperature));
        OnPropertyChanged(nameof(WeatherCondition));
        OnPropertyChanged(nameof(IsWeatherLoading));
        OnPropertyChanged(nameof(WeatherStatusText));
        OnPropertyChanged(nameof(WeatherRecommendations));
        OnPropertyChanged(nameof(HasWeatherRecommendations));
        OnPropertyChanged(nameof(WeatherHeadlineText));
        OnPropertyChanged(nameof(WeatherCityCompactText));
        OnPropertyChanged(nameof(WeatherCompactSummaryText));
        OnPropertyChanged(nameof(WeatherRecommendationCountText));
        OnPropertyChanged(nameof(PrimaryWeatherRecommendation));
        OnPropertyChanged(nameof(WeatherRecommendationHintText));
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

    private string BuildNoRecommendationHint()
    {
        if (Outfits.Count == 0)
            return "还没有搭配，先建 2-3 套常穿组合，推荐就会马上有感觉。";

        if (Outfits.All(outfit => outfit.Season == Domain.Enums.Season.Unspecified))
            return "搭配还没补季节，先把常穿那几套标清楚，推荐会准很多。";

        var suggestedSeason = ResolveSuggestedSeason(WeatherTemperature);
        if (!Outfits.Any(outfit => outfit.Season is Domain.Enums.Season.AllSeason || outfit.Season == suggestedSeason))
            return $"现在这类温度还缺少 {GetSeasonName(suggestedSeason)} 搭配，补几套会更好用。";

        return "现有搭配里暂时没有特别突出的那套，先从最近没穿过的开始挑。";
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

    private static Domain.Enums.Season ResolveSuggestedSeason(int temperature)
    {
        return temperature switch
        {
            >= 26 => Domain.Enums.Season.Summer,
            >= 16 => Domain.Enums.Season.Spring,
            >= 10 => Domain.Enums.Season.Autumn,
            _ => Domain.Enums.Season.Winter
        };
    }

    private static string GetSeasonName(Domain.Enums.Season season)
    {
        return season switch
        {
            Domain.Enums.Season.Spring => "春季",
            Domain.Enums.Season.Summer => "夏季",
            Domain.Enums.Season.Autumn => "秋季",
            Domain.Enums.Season.Winter => "冬季",
            Domain.Enums.Season.AllSeason => "四季",
            _ => "当前"
        };
    }
}
