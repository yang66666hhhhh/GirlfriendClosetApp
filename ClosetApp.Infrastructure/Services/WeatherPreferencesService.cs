using System.Text.Json;

namespace ClosetApp.Infrastructure.Services;

public class WeatherPreferencesService : IWeatherPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly string _filePath;

    public WeatherPreferencesService(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppPaths.BaseDir, "weather-settings.json");
    }

    public async Task<WeatherPreferences> GetAsync()
    {
        await Gate.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
                return CreateDefaultPreferences();

            await using var stream = File.OpenRead(_filePath);
            var preferences = await JsonSerializer.DeserializeAsync<WeatherPreferences>(stream, JsonOptions);
            return Normalize(preferences);
        }
        catch
        {
            return CreateDefaultPreferences();
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task SaveAsync(WeatherPreferences preferences)
    {
        var normalized = Normalize(preferences);

        await Gate.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, normalized, JsonOptions);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static WeatherPreferences CreateDefaultPreferences()
    {
        return new WeatherPreferences();
    }

    private static WeatherPreferences Normalize(WeatherPreferences? preferences)
    {
        var city = preferences?.DefaultCity?.Trim();
        return new WeatherPreferences
        {
            DefaultCity = string.IsNullOrWhiteSpace(city) ? "Shanghai" : city
        };
    }
}
