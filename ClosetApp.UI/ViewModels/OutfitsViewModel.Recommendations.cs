using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class OutfitsViewModel
{
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
        nameof(TodayHeroPrimaryActionText),
        nameof(TodayHeroStatusSummaryText),
        nameof(SecondaryWeatherRecommendationSectionBody)
    ];

    private readonly IOutfitRecommendationService _outfitRecommendationService;
    private readonly IWeatherService _weatherService;
    private readonly IWeatherPreferencesService _weatherPreferencesService;
    private readonly IRecommendationPreferencesService _recommendationPreferencesService;
    private readonly GetTodayRecommendations _getTodayRecommendations;
    private RecommendationDebugDto? _cachedBestDebug;
    private readonly Dictionary<Guid, RecommendationDebugDto> _cachedOutfitDebugs = new();

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

    public string WeatherHeadlineText => $"{WeatherCity} · {WeatherTemperature}°C · {WeatherCondition}";
    public string WeatherCityCompactText => OutfitPresentationText.BuildCompactWeatherCity(WeatherCity);
    public string WeatherCompactSummaryText => OutfitPresentationText.BuildCompactWeatherSummary(WeatherTemperature, WeatherCondition);
    public bool HasWeatherRecommendations => WeatherRecommendations.Count > 0;
    public bool HasPrimaryWeatherRecommendation => PrimaryWeatherRecommendation != null;
    public bool HasSecondaryWeatherRecommendations => SecondaryWeatherRecommendations.Count > 0;
    public string WeatherRecommendationCountText => OutfitPresentationText.BuildRecommendationCountText(WeatherRecommendations);
    public RecommendedOutfitDto? PrimaryWeatherRecommendation => WeatherRecommendations.FirstOrDefault();
    public IReadOnlyList<RecommendedOutfitDto> SecondaryWeatherRecommendations => WeatherRecommendations.Skip(1).Take(2).ToList();
    public bool CanRefreshWeatherRecommendations => !IsWeatherLoading;
    public string RefreshWeatherButtonText => IsWeatherLoading ? "刷新中..." : "刷新天气推荐";
    public bool HasRecommendationReadiness => RecommendationReadiness != null;
    public bool HasRecommendationGap => RecommendationReadiness?.HasGap ?? false;
    public string RecommendationReadinessTitle => RecommendationReadiness?.Title ?? "推荐准备度";
    public string RecommendationReadinessDetail => RecommendationReadiness?.Detail ?? "刷新天气后会整理当前搭配是否够用。";
    public string RecommendationReadinessBadgeText => OutfitPresentationText.BuildRecommendationReadinessBadgeText(HasRecommendationGap);
    public string RecommendationReadinessCountText => OutfitPresentationText.BuildRecommendationReadinessCountText(RecommendationReadiness);
    public string RecommendationMissingSeasonText => OutfitPresentationText.BuildRecommendationMissingSeasonText(RecommendationReadiness, HasRecommendationGap);
    public string WeatherRecommendationHintText => OutfitPresentationText.BuildWeatherRecommendationHintText(WeatherRecommendations, RecommendationReadinessDetail);
    public string TodayHeroRecommendationNameText => HasPrimaryWeatherRecommendation
        ? PrimaryWeatherRecommendation!.Name
        : "今天还没有合适推荐";
    public string TodayHeroRecommendationSupportText => HasPrimaryWeatherRecommendation
        ? OutfitPresentationText.BuildTodayHeroSupportText(
            PrimaryWeatherRecommendation!,
            HasTodayWornRecords,
            TodayWornCount)
        : HasTodayWornRecords
            ? $"{TodayWornStatusText}，{RecommendationReadinessDetail}"
            : RecommendationReadinessDetail;
    public string TodayHeroPrimaryActionText => HasPrimaryWeatherRecommendation
        ? OutfitPresentationText.BuildTodayHeroPrimaryActionText(PrimaryWeatherRecommendation, HasTodayWornRecords)
        : "去新建一套";
    public string TodayHeroStatusSummaryText => HasRecommendationGap
        ? $"{RecommendationReadinessTitle} · {RecommendationMissingSeasonText}"
        : $"{RecommendationReadinessTitle} · {WeatherStatusText}";
    public string SecondaryWeatherRecommendationSectionBody => HasSecondaryWeatherRecommendations
        ? "保留两套轻候选，换场景或换心情时不用重新翻。"
        : "当前没有额外候选。";

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
        if (recommendation == null)
            return;

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

    private async Task RefreshRecommendationsForCurrentWeatherAsync()
    {
        try
        {
            var recommendationPreferences = await _recommendationPreferencesService.GetAsync();
            var request = new TodayRecommendationRequest(
                WeatherCity,
                WeatherTemperature,
                WeatherCondition,
                IsWeatherFromApi: !string.Equals(WeatherCondition, "天气暂缺", StringComparison.Ordinal),
                recommendationPreferences.DefaultScene,
                recommendationPreferences.AvoidWornToday,
                recommendationPreferences.RotationStrategy);

            var result = await _getTodayRecommendations.ExecuteAsync(request);
            WeatherRecommendations = result.Recommendations;
            RecommendationReadiness = result.Readiness;
            WeatherStatusText = result.StatusText;
            NotifyWeatherStateChanged();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to refresh recommendations with cached weather context");
        }
    }

    private async Task RefreshWeatherRecommendationsInBackgroundAsync()
    {
        try
        {
            await RefreshWeatherRecommendationsAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to refresh weather recommendations in background");
        }
    }

    private async Task RefreshRecommendationsForCurrentWeatherInBackgroundAsync()
    {
        try
        {
            await RefreshRecommendationsForCurrentWeatherAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to refresh cached weather recommendations in background");
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

    private void InvalidateRecommendationDebugCache()
    {
        _cachedBestDebug = null;
        _cachedOutfitDebugs.Clear();
    }
}
