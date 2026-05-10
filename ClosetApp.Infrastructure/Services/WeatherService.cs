namespace ClosetApp.Infrastructure.Services;

public class WeatherService : IWeatherService
{
    public Task<WeatherInfo?> GetCurrentWeatherAsync(string city)
    {
        return Task.FromResult<WeatherInfo?>(new WeatherInfo
        {
            City = city,
            Temperature = 22,
            Condition = "晴",
            Humidity = 50
        });
    }
}