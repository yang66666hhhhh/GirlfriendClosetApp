using System.Text.Json;
using ClosetApp.Application.Interfaces;

namespace ClosetApp.Infrastructure.Services;

public class WeatherPreferencesService : IWeatherPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly string _filePath;
    private readonly UserScopedSettingsPath _settingsPath;

    public WeatherPreferencesService(string? filePath = null, ICurrentUserContext? currentUserContext = null)
    {
        _filePath = filePath ?? Path.Combine(AppPaths.BaseDir, "weather-settings.json");
        _settingsPath = new UserScopedSettingsPath(currentUserContext, _filePath);
    }

    public async Task<WeatherPreferences> GetAsync()
    {
        await Gate.WaitAsync();
        try
        {
            await _settingsPath.MigrateGlobalFileIfNeededAsync();
            var path = await _settingsPath.ResolveAsync();
            if (!File.Exists(path))
                return CreateDefaultPreferences();

            await using var stream = File.OpenRead(path);
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
            var path = await _settingsPath.ResolveAsync();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using var stream = File.Create(path);
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
