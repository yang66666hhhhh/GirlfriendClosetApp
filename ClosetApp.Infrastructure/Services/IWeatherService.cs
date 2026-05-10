namespace ClosetApp.Infrastructure.Services;

public class WeatherInfo
{
    public string City { get; set; } = string.Empty;
    public int Temperature { get; set; }
    public string Condition { get; set; } = string.Empty;
    public int Humidity { get; set; }
}

public interface IWeatherService
{
    Task<WeatherInfo?> GetCurrentWeatherAsync(string city);
}