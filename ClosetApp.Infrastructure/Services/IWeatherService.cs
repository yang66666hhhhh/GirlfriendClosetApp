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

public class WeatherPreferences
{
    public string DefaultCity { get; set; } = "Shanghai";
}

public interface IWeatherService
{
    Task<WeatherInfo?> GetCurrentWeatherAsync(string city);
}

public interface IWeatherPreferencesService
{
    Task<WeatherPreferences> GetAsync();
    Task SaveAsync(WeatherPreferences preferences);
}
