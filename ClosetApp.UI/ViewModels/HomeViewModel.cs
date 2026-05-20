using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Infrastructure.Services;
using System.Collections.ObjectModel;

namespace ClosetApp.UI.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IOutfitService _outfitService;
    private readonly IOutfitRecommendationService _recommendationService;
    private readonly IWeatherService _weatherService;
    private readonly IWeatherPreferencesService _weatherPreferencesService;

    [ObservableProperty]
    private ObservableCollection<Outfit> _recommendations = new();

    [ObservableProperty]
    private ObservableCollection<Outfit> _recentOutfits = new();

    [ObservableProperty]
    private int _temperature = 22;

    [ObservableProperty]
    private string _weatherCondition = "晴";

    [ObservableProperty]
    private string _weatherCity = "Shanghai";

    [ObservableProperty]
    private string _greeting = "你好";

    [ObservableProperty]
    private bool _isLoading;

    public HomeViewModel(
        IOutfitService outfitService,
        IOutfitRecommendationService recommendationService,
        IWeatherService weatherService,
        IWeatherPreferencesService weatherPreferencesService)
    {
        _outfitService = outfitService;
        _recommendationService = recommendationService;
        _weatherService = weatherService;
        _weatherPreferencesService = weatherPreferencesService;
        UpdateGreeting();
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        Greeting = hour switch
        {
            >= 5 and < 12 => "早上好",
            >= 12 and < 14 => "中午好",
            >= 14 and < 18 => "下午好",
            _ => "晚上好"
        };
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            await LoadWeatherAsync();

            var recent = await _outfitService.GetRecentlyWornOutfitsAsync(5);
            RecentOutfits = new ObservableCollection<Outfit>(recent);

            var recs = await _recommendationService.GetRecommendationsByRuleAsync(Temperature, null);
            Recommendations = new ObservableCollection<Outfit>(recs);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshRecommendationsAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadWeatherAsync()
    {
        var preferences = await _weatherPreferencesService.GetAsync();
        WeatherCity = preferences.DefaultCity;

        var weather = await _weatherService.GetCurrentWeatherAsync(WeatherCity);
        if (weather == null)
            return;

        WeatherCity = weather.City;
        Temperature = weather.Temperature;
        WeatherCondition = weather.Condition;
    }
}
