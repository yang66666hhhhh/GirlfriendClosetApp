using System.Windows;
using System.Windows.Controls;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Settings;

public partial class WeatherPreferencesSettingsPanel : UserControl
{
    private readonly SettingsViewModel _viewModel;

    public WeatherPreferencesSettingsPanel()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
    }

    public event EventHandler? OutfitsRefreshRequested;

    public Task RefreshAsync()
    {
        return _viewModel.RefreshWeatherAsync(showStatus: false);
    }

    private async void RefreshWeather_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshWeatherAsync(showStatus: true);
    }

    private async void SaveWeatherCity_Click(object sender, RoutedEventArgs e)
    {
        var city = _viewModel.WeatherCity.Trim();
        await _viewModel.SaveWeatherCityAsync(city);
        if (string.IsNullOrWhiteSpace(city))
            TxtWeatherCity.Focus();
        OutfitsRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void SaveRecommendationPreferences_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveRecommendationPreferencesAsync();
        OutfitsRefreshRequested?.Invoke(this, EventArgs.Empty);
    }
}
