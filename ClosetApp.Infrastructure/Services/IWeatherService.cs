namespace ClosetApp.Infrastructure.Services;

public class WeatherInfo
{
    public string City { get; set; } = string.Empty;
    public int Temperature { get; set; }
    public string Condition { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public DateTimeOffset? ObservedAt { get; set; }
}

public sealed record WeatherCitySuggestion(string DisplayName);

public class WeatherPreferences
{
    public string DefaultCity { get; set; } = "Shanghai";
}

public interface IWeatherService
{
    Task<WeatherInfo?> GetCurrentWeatherAsync(string city);
    Task<IReadOnlyList<WeatherCitySuggestion>> SearchCitiesAsync(string query, int maxResults = 6);
    int GetFallbackTemperature(DateTimeOffset? date = null);
}

public interface IWeatherPreferencesService
{
    Task<WeatherPreferences> GetAsync();
    Task SaveAsync(WeatherPreferences preferences);
}
