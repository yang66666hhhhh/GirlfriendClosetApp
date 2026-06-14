using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.ViewModels;

namespace ClosetApp.UI.Components.Settings;

public partial class WeatherPreferencesSettingsPanel : UserControl
{
    public WeatherPreferencesSettingsPanel()
    {
        InitializeComponent();
    }

    public event EventHandler? OutfitsRefreshRequested;

    private SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

    public Task RefreshAsync()
    {
        return ViewModel?.RefreshWeatherAsync(showStatus: false) ?? Task.CompletedTask;
    }

    private async void RefreshWeather_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
            return;

        await ViewModel.RefreshWeatherAsync(showStatus: true);
    }

    private async void SaveWeatherCity_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
            return;

        var city = ViewModel.WeatherCity.Trim();
        await ViewModel.SaveWeatherCityAsync(city);
        if (string.IsNullOrWhiteSpace(city))
            TxtWeatherCity.Focus();
        OutfitsRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void SaveRecommendationPreferences_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
            return;

        await ViewModel.SaveRecommendationPreferencesAsync();
        OutfitsRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private void WeatherCitySuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || sender is not ComboBox { SelectedItem: WeatherCitySuggestion suggestion })
            return;

        Dispatcher.BeginInvoke(() =>
        {
            if (ViewModel == null)
                return;

            ViewModel.SelectWeatherCitySuggestion(suggestion);
            TxtWeatherCity.Text = suggestion.DisplayName;
            TxtWeatherCity.IsDropDownOpen = false;
            TxtWeatherCity.Focus();

            if (TxtWeatherCity.Template.FindName("PART_EditableTextBox", TxtWeatherCity) is TextBox editableTextBox)
            {
                editableTextBox.CaretIndex = editableTextBox.Text.Length;
            }
        }, DispatcherPriority.Background);
    }

    private void WeatherCityInput_LostFocus(object sender, RoutedEventArgs e)
    {
        ViewModel?.HideWeatherCitySuggestions();
    }
}
